using Inventory.API.ExceptionHandlers;
using Inventory.API.Middlewares;
using Inventory.API.Observability;
using Inventory.API.Grpc.Services;
using Inventory.Application;
using Inventory.Application.Abstractions.Observability;
using Inventory.Infrastructure;
using Inventory.Infrastructure.Observability;
using Inventory.Persistence;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.ConfigureInventorySerilog(builder.Configuration, builder.Environment);

builder.Services.AddScoped<ICorrelationContextAccessor, CorrelationContextAccessor>();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddControllers();
builder.Services.AddGrpc();
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
app.MapGrpcService<InventoryReservationGrpcService>();

app.Run();
