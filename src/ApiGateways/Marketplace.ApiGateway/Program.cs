using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.HttpOverrides;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using System.Threading.RateLimiting;
using Yarp.ReverseProxy.Transforms;

const string GatewayCorsPolicyName = "GatewayCors";
const string GatewayRateLimitPolicyName = "GatewayPerClientRateLimit";

var builder = WebApplication.CreateBuilder(args);
var authenticationOptions = builder.Configuration
    .GetSection("Gateway:Authentication")
    .Get<GatewayAuthenticationOptions>() ?? new GatewayAuthenticationOptions();

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

if (authenticationOptions.Enabled)
{
    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options => GatewayAuthentication.ConfigureJwtBearer(options, authenticationOptions));
}

builder.Services.AddAuthorization(options =>
{
    GatewayAuthorization.ConfigurePolicies(options, authenticationOptions.Enabled);
});

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(
        serviceName: "Marketplace.ApiGateway",
        serviceVersion: "1.0.0",
        serviceInstanceId: Environment.MachineName))
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddPrometheusExporter());

builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddTransforms(transformBuilderContext =>
    {
        transformBuilderContext.AddRequestTransform(transformContext =>
        {
            GatewayIdentityHeaders.Apply(transformContext.ProxyRequest, transformContext.HttpContext.User);
            return ValueTask.CompletedTask;
        });
    });

builder.Services.AddHealthChecks();

var app = builder.Build();

app.MapPrometheusScrapingEndpoint();

app.UseForwardedHeaders();
app.UseGatewaySecurityHeaders();
app.UseCors(GatewayCorsPolicyName);
app.UseRateLimiter();
if (authenticationOptions.Enabled)
{
    app.UseAuthentication();
}
app.UseAuthorization();

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

internal sealed class GatewayAuthenticationOptions
{
    public bool Enabled { get; init; } = true;

    public string? Authority { get; init; }

    public string Audience { get; init; } = "marketplace-api";

    public string? ValidIssuer { get; init; }

    public bool RequireHttpsMetadata { get; init; } = true;

    public int ClockSkewMinutes { get; init; } = 2;
}

internal static class GatewayAuthorizationPolicies
{
    public const string Authenticated = "authenticated";
    public const string CatalogManagement = "catalog-management";
    public const string InventoryManagement = "inventory-management";
    public const string Checkout = "checkout";
    public const string CustomerOrAdmin = "customer-or-admin";
    public const string Support = "support";
}

internal static class GatewayRoles
{
    public const string Customer = "customer";
    public const string Admin = "admin";
    public const string CatalogManager = "catalog-manager";
    public const string InventoryManager = "inventory-manager";
    public const string Support = "support";
}

internal static class GatewayAuthorization
{
    public static void ConfigurePolicies(AuthorizationOptions options, bool authenticationEnabled)
    {
        if (!authenticationEnabled)
        {
            AddDisabledPolicy(options, GatewayAuthorizationPolicies.Authenticated);
            AddDisabledPolicy(options, GatewayAuthorizationPolicies.CatalogManagement);
            AddDisabledPolicy(options, GatewayAuthorizationPolicies.InventoryManagement);
            AddDisabledPolicy(options, GatewayAuthorizationPolicies.Checkout);
            AddDisabledPolicy(options, GatewayAuthorizationPolicies.CustomerOrAdmin);
            AddDisabledPolicy(options, GatewayAuthorizationPolicies.Support);
            return;
        }

        options.AddPolicy(GatewayAuthorizationPolicies.Authenticated, policy =>
            policy.RequireAuthenticatedUser());
        options.AddPolicy(GatewayAuthorizationPolicies.CatalogManagement, policy =>
            policy.RequireRole(GatewayRoles.Admin, GatewayRoles.CatalogManager));
        options.AddPolicy(GatewayAuthorizationPolicies.InventoryManagement, policy =>
            policy.RequireRole(GatewayRoles.Admin, GatewayRoles.InventoryManager));
        options.AddPolicy(GatewayAuthorizationPolicies.Checkout, policy =>
            policy.RequireRole(GatewayRoles.Customer, GatewayRoles.Admin));
        options.AddPolicy(GatewayAuthorizationPolicies.CustomerOrAdmin, policy =>
            policy.RequireRole(GatewayRoles.Customer, GatewayRoles.Admin));
        options.AddPolicy(GatewayAuthorizationPolicies.Support, policy =>
            policy.RequireRole(GatewayRoles.Admin, GatewayRoles.Support));
    }

    private static void AddDisabledPolicy(AuthorizationOptions options, string policyName)
    {
        options.AddPolicy(policyName, policy => policy.RequireAssertion(_ => true));
    }
}

internal static class GatewayAuthentication
{
    public static void ConfigureJwtBearer(JwtBearerOptions options, GatewayAuthenticationOptions authenticationOptions)
    {
        if (authenticationOptions.Enabled && string.IsNullOrWhiteSpace(authenticationOptions.Authority))
        {
            throw new InvalidOperationException(
                "Gateway authentication is enabled, but Gateway:Authentication:Authority is not configured.");
        }

        options.RequireHttpsMetadata = authenticationOptions.RequireHttpsMetadata;
        options.Authority = NullIfWhiteSpace(authenticationOptions.Authority);
        options.Audience = authenticationOptions.Audience;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = !string.IsNullOrWhiteSpace(authenticationOptions.ValidIssuer),
            ValidIssuer = NullIfWhiteSpace(authenticationOptions.ValidIssuer),
            ValidateAudience = true,
            ValidAudience = authenticationOptions.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(authenticationOptions.ClockSkewMinutes),
            RoleClaimType = ClaimTypes.Role
        };
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                AddKeycloakRealmRoles(context.Principal);
                return Task.CompletedTask;
            }
        };
    }

    private static void AddKeycloakRealmRoles(ClaimsPrincipal? principal)
    {
        if (principal?.Identity is not ClaimsIdentity identity)
        {
            return;
        }

        var realmAccess = principal.FindFirst("realm_access")?.Value;
        if (string.IsNullOrWhiteSpace(realmAccess))
        {
            return;
        }

        using var document = JsonDocument.Parse(realmAccess);
        if (!document.RootElement.TryGetProperty("roles", out var rolesElement) ||
            rolesElement.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var roleElement in rolesElement.EnumerateArray())
        {
            var role = roleElement.GetString();
            if (!string.IsNullOrWhiteSpace(role) &&
                !principal.HasClaim(ClaimTypes.Role, role))
            {
                identity.AddClaim(new Claim(ClaimTypes.Role, role));
            }
        }
    }

    private static string? NullIfWhiteSpace(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }
}

internal static class GatewayIdentityHeaders
{
    public const string UserId = "X-User-Id";
    public const string UserEmail = "X-User-Email";
    public const string UserRoles = "X-User-Roles";

    private static readonly string[] HeaderNames = [UserId, UserEmail, UserRoles];

    public static void Apply(HttpRequestMessage proxyRequest, ClaimsPrincipal user)
    {
        RemoveClientSuppliedIdentityHeaders(proxyRequest);

        if (user.Identity?.IsAuthenticated != true)
        {
            return;
        }

        AddHeaderIfPresent(proxyRequest, UserId, ResolveUserId(user));
        AddHeaderIfPresent(proxyRequest, UserEmail, ResolveUserEmail(user));

        var roles = user.FindAll(ClaimTypes.Role)
            .Select(claim => claim.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (roles.Length > 0)
        {
            proxyRequest.Headers.TryAddWithoutValidation(UserRoles, string.Join(',', roles));
        }
    }

    private static void RemoveClientSuppliedIdentityHeaders(HttpRequestMessage proxyRequest)
    {
        foreach (var headerName in HeaderNames)
        {
            proxyRequest.Headers.Remove(headerName);
        }
    }

    private static string? ResolveUserId(ClaimsPrincipal user)
    {
        return user.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? user.FindFirstValue("sub");
    }

    private static string? ResolveUserEmail(ClaimsPrincipal user)
    {
        return user.FindFirstValue(ClaimTypes.Email)
               ?? user.FindFirstValue("email")
               ?? user.FindFirstValue("preferred_username");
    }

    private static void AddHeaderIfPresent(HttpRequestMessage proxyRequest, string headerName, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            proxyRequest.Headers.TryAddWithoutValidation(headerName, value);
        }
    }
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
