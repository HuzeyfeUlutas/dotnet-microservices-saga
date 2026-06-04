using Microsoft.AspNetCore.HttpOverrides;
using System.Threading.RateLimiting;

const string GatewayCorsPolicyName = "GatewayCors";
const string GatewayRateLimitPolicyName = "GatewayPerClientRateLimit";

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddCors(options =>
{
    options.AddPolicy(GatewayCorsPolicyName, policy =>
    {
        var allowedOrigins = builder.Configuration
            .GetSection("Gateway:Cors:AllowedOrigins")
            .Get<string[]>() ?? [];

        if (allowedOrigins.Length == 0)
        {
            policy.SetIsOriginAllowed(_ => false);
            return;
        }

        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddRateLimiter(options =>
{
    var rateLimitOptions = builder.Configuration
        .GetSection("Gateway:RateLimiting")
        .Get<GatewayRateLimitOptions>() ?? new GatewayRateLimitOptions();

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy(GatewayRateLimitPolicyName, httpContext =>
    {
        var partitionKey = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown-client";

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey,
            _ => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = rateLimitOptions.PermitLimit,
                QueueLimit = rateLimitOptions.QueueLimit,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                Window = TimeSpan.FromSeconds(rateLimitOptions.WindowSeconds)
            });
    });
});

builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseForwardedHeaders();
app.UseGatewaySecurityHeaders();
app.UseCors(GatewayCorsPolicyName);
app.UseRateLimiter();

app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");

app.MapGet("/", () => Results.Ok(new
{
    service = "Marketplace API Gateway",
    status = "Running"
}));

app.MapReverseProxy().RequireRateLimiting(GatewayRateLimitPolicyName);

app.Run();

public partial class Program;

internal sealed class GatewayRateLimitOptions
{
    public int PermitLimit { get; init; } = 100;

    public int QueueLimit { get; init; }

    public int WindowSeconds { get; init; } = 60;
}

internal static class SecurityHeadersApplicationBuilderExtensions
{
    public static IApplicationBuilder UseGatewaySecurityHeaders(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            context.Response.OnStarting(() =>
            {
                var headers = context.Response.Headers;

                headers["X-Content-Type-Options"] = "nosniff";
                headers["X-Frame-Options"] = "DENY";
                headers["Referrer-Policy"] = "no-referrer";
                headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";

                return Task.CompletedTask;
            });

            await next();
        });
    }
}
