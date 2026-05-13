using Catalog.API.Observability;
using Catalog.Application.Common.Exceptions;
using Catalog.Domain.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using PostgresErrorCodes = Npgsql.PostgresErrorCodes;

namespace Catalog.API.ExceptionHandlers;

public class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var correlationId = httpContext.Items[CorrelationConstants.ItemKey] as string;

        logger.LogError(
            exception,
            "Unhandled exception occurred while processing request. CorrelationId: {CorrelationId}",
            correlationId);

        var (statusCode, title, detail, extensions) = MapException(httpContext, exception);

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path
        };

        foreach (var extension in extensions)
        {
            problemDetails.Extensions[extension.Key] = extension.Value;
        }

        problemDetails.Extensions["traceId"] = httpContext.TraceIdentifier;
        problemDetails.Extensions["correlationId"] = correlationId;

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }

    private static (int StatusCode, string Title, string Detail, Dictionary<string, object?> Extensions) MapException(
        HttpContext httpContext,
        Exception exception)
    {
        return exception switch
        {
            ValidationException validationException => (
                StatusCodes.Status400BadRequest,
                "Validation failed",
                "One or more validation errors occurred.",
                new Dictionary<string, object?>
                {
                    ["errors"] = validationException.Errors
                        .GroupBy(x => x.PropertyName)
                        .ToDictionary(
                            group => group.Key,
                            group => group.Select(x => x.ErrorMessage).ToArray())
                }),
            NotFoundException => (
                StatusCodes.Status404NotFound,
                "Resource not found",
                exception.Message,
                new Dictionary<string, object?>()),
            ConflictException => (
                StatusCodes.Status409Conflict,
                "Conflict",
                exception.Message,
                new Dictionary<string, object?>()),
            DbUpdateException dbUpdateException when IsUniqueViolation(dbUpdateException) => (
                StatusCodes.Status409Conflict,
                "Conflict",
                MapUniqueViolationDetail(dbUpdateException),
                new Dictionary<string, object?>()),
            ForbiddenException => (
                StatusCodes.Status403Forbidden,
                "Forbidden",
                exception.Message,
                new Dictionary<string, object?>()),
            DomainException => (
                StatusCodes.Status400BadRequest,
                "Domain rule violation",
                exception.Message,
                new Dictionary<string, object?>()),
            _ => (
                StatusCodes.Status500InternalServerError,
                "Internal Server Error",
                "An unexpected error occurred.",
                new Dictionary<string, object?>())
        };
    }

    private static bool IsUniqueViolation(DbUpdateException exception)
    {
        return exception.InnerException is PostgresException postgresException
               && postgresException.SqlState == PostgresErrorCodes.UniqueViolation;
    }

    private static string MapUniqueViolationDetail(DbUpdateException exception)
    {
        if (exception.InnerException is not PostgresException postgresException)
        {
            return "A unique constraint was violated.";
        }

        return postgresException.ConstraintName switch
        {
            "IX_brands_Name_CI" => "Brand name already exists.",
            "IX_categories_Name_CI" => "Category name already exists.",
            "IX_product_variants_ProductId_Sku_CI" => "Variant SKU already exists for this product.",
            "IX_product_variants_ProductId_Sku" => "Variant SKU already exists for this product.",
            _ => "A unique constraint was violated."
        };
    }
}
