using Inventory.Application.Abstractions.Messaging;
using Inventory.Application.Abstractions.Observability;
using Inventory.Infrastructure.Configuration;
using Inventory.Infrastructure.Messaging;
using Inventory.Infrastructure.Messaging.Consumers;
using Inventory.Infrastructure.Observability;
using Inventory.Persistence.Context;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Inventory.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IIntegrationEventPublisher, MassTransitIntegrationEventPublisher>();
        services.AddSingleton<IInventoryMetrics, InventoryMetrics>();
        services.AddInventoryTracing(configuration);
        services.AddInventoryHealthChecks(configuration);

        services.AddMassTransit(x =>
        {
            x.SetKebabCaseEndpointNameFormatter();
            x.AddConsumer<CommitStockRequestedConsumer>();
            x.AddConsumer<ReleaseStockRequestedConsumer>();
            x.AddConsumer<ReverseCommittedStockRequestedConsumer>();

            x.AddEntityFrameworkOutbox<InventoryDbContext>(o =>
            {
                o.UsePostgres();
                o.UseBusOutbox();
            });

            x.AddConfigureEndpointsCallback((context, _, cfg) =>
            {
                cfg.UseEntityFrameworkOutbox<InventoryDbContext>(context);
            });

            x.UsingRabbitMq((context, cfg) =>
            {
                var options = BuildRabbitMqOptions(configuration);

                cfg.Host(options.Host, options.Port, options.VirtualHost, host =>
                {
                    host.Username(options.Username);
                    host.Password(options.Password);
                });

                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }

    private static RabbitMqOptions BuildRabbitMqOptions(IConfiguration configuration)
    {
        var section = configuration.GetSection(RabbitMqOptions.SectionName);

        return new RabbitMqOptions
        {
            Host = section["Host"] ?? "localhost",
            Port = ushort.TryParse(section["Port"], out var port) ? port : (ushort)5672,
            VirtualHost = section["VirtualHost"] ?? "/",
            Username = section["Username"] ?? "guest",
            Password = section["Password"] ?? "guest"
        };
    }
}
