using Order.API.ExceptionHandlers;
using Order.API.Middlewares;
using Order.API.Observability;
using Order.Application;
using Order.Application.Abstractions.Observability;
using Order.Infrastructure;
using Order.Infrastructure.Observability;
using Order.Persistence;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.ConfigureOrderSerilog(builder.Configuration, builder.Environment);

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

app.UseHttpsRedirection();

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

app.MapPrometheusScrapingEndpoint();
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("live"),
    ResponseWriter = HealthCheckResponseWriter.WriteAsync
});
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready"),
    ResponseWriter = HealthCheckResponseWriter.WriteAsync
});

app.MapControllers();

app.Run();
