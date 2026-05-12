using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Payment.Application.Common.Exceptions;
using Payment.Domain.Exceptions;

namespace Payment.API.ExceptionHandlers;

public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        logger.LogError(
            exception,
            "Unhandled exception occurred while processing request. TraceId: {TraceId}",
            httpContext.TraceIdentifier);

        var (statusCode, title, detail, extensions) = MapException(exception);

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

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }

    private static (int StatusCode, string Title, string Detail, Dictionary<string, object?> Extensions) MapException(
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
}
