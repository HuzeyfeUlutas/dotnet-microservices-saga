using Catalog.API.Observability;
using Catalog.Application.Abstractions.Observability;
using Serilog.Context;

namespace Catalog.API.Middlewares;

public sealed class CorrelationMiddleware(
    RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, ICorrelationContextAccessor correlationContextAccessor)
    {
        var correlationId = ResolveCorrelationId(context);
        var traceId = System.Diagnostics.Activity.Current?.TraceId.ToString();

        correlationContextAccessor.CorrelationId = correlationId;
        context.Items[CorrelationConstants.ItemKey] = correlationId;

        context.Response.OnStarting(() =>
        {
            context.Response.Headers[CorrelationConstants.HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using (LogContext.PushProperty("CorrelationId", correlationId))
        using (LogContext.PushProperty("RequestPath", context.Request.Path.Value))
        using (LogContext.PushProperty("RequestMethod", context.Request.Method))
        using (LogContext.PushProperty("TraceId", traceId))
        {
            await next(context);
        }
    }

    private static string ResolveCorrelationId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(CorrelationConstants.HeaderName, out var headerValue))
        {
            var correlationId = headerValue.ToString().Trim();
            if (!string.IsNullOrWhiteSpace(correlationId))
            {
                return correlationId;
            }
        }

        return Guid.NewGuid().ToString("D");
    }
}
