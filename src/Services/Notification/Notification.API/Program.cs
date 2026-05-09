using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Notification.API.ExceptionHandlers;
using Notification.API.Middlewares;
using Notification.API.Observability;
using Notification.Application;
using Notification.Application.Abstractions.Observability;
using Notification.Infrastructure;
using Notification.Infrastructure.Observability;
using Notification.Persistence;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.ConfigureNotificationSerilog(builder.Configuration, builder.Environment);

builder.Services.AddScoped<ICorrelationContextAccessor, CorrelationContextAccessor>();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddApplication();
builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<CorrelationMiddleware>();

app.UseExceptionHandler();
app.UseSerilogRequestLogging(options =>
{
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("CorrelationId", httpContext.Items[CorrelationConstants.ItemKey]);
        diagnosticContext.Set("RequestPath", httpContext.Request.Path.Value);
        diagnosticContext.Set("RequestMethod", httpContext.Request.Method);
        diagnosticContext.Set("TraceId", System.Diagnostics.Activity.Current?.TraceId.ToString());
    };
});

app.UseHttpsRedirection();

app.MapPrometheusScrapingEndpoint();
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("live")
});
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready")
});

app.MapControllers();

app.Run();
