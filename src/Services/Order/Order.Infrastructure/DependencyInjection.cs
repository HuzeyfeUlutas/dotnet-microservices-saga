using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Order.Application.Abstractions.Messaging;
using Order.Application.Abstractions.Observability;
using Order.Application.Abstractions.Services;
using Order.Infrastructure.Configuration;
using Order.Infrastructure.Messaging;
using Order.Infrastructure.Messaging.Consumers;
using Order.Infrastructure.Services;
using Order.Infrastructure.Observability;
using Order.Persistence.Context;
using Marketplace.Grpc.Catalog.V1;
using Marketplace.Grpc.Inventory.V1;
using Marketplace.Grpc.Payment.V1;

namespace Order.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IIntegrationEventPublisher, MassTransitIntegrationEventPublisher>();
        services.AddScoped<IStockReservationRollbackPublisher, MassTransitStockReservationRollbackPublisher>();

        var serviceEndpointOptions = BuildServiceEndpointOptions(configuration);
        services.AddSingleton(serviceEndpointOptions);

        services.AddGrpcClient<CatalogPurchaseInfo.CatalogPurchaseInfoClient>(client =>
        {
            client.Address = new Uri(serviceEndpointOptions.CatalogGrpcUrl);
        });
        services.AddScoped<ICatalogPurchaseInfoClient, CatalogPurchaseInfoGrpcClient>();

        services.AddGrpcClient<InventoryReservation.InventoryReservationClient>(client =>
        {
            client.Address = new Uri(serviceEndpointOptions.InventoryGrpcUrl);
        });
        services.AddScoped<IInventoryReservationClient, InventoryReservationClient>();

        services.AddGrpcClient<PaymentInitiation.PaymentInitiationClient>(client =>
        {
            client.Address = new Uri(serviceEndpointOptions.PaymentGrpcUrl);
        });
        services.AddScoped<IPaymentClient, PaymentClient>();

        services.AddMassTransit(x =>
        {
            x.SetKebabCaseEndpointNameFormatter();
            x.AddConsumer<PaymentAuthorizedConsumer>();
            x.AddConsumer<PaymentAuthorizationFailedConsumer>();
            x.AddConsumer<PaymentCapturedConsumer>();
            x.AddConsumer<PaymentCaptureFailedConsumer>();
            x.AddConsumer<StockCommittedConsumer>();
            x.AddConsumer<StockCommitFailedConsumer>();
            x.AddConsumer<StockReleasedConsumer>();
            x.AddConsumer<StockReleaseFailedConsumer>();

            x.AddEntityFrameworkOutbox<OrderDbContext>(o =>
            {
                o.UsePostgres();
                o.UseBusOutbox();
            });

            x.AddConfigureEndpointsCallback((context, _, cfg) =>
            {
                cfg.UseEntityFrameworkOutbox<OrderDbContext>(context);
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

        services.AddOrderTracing(configuration);
        services.AddOrderHealthChecks(configuration);

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

    private static ServiceEndpointOptions BuildServiceEndpointOptions(IConfiguration configuration)
    {
        var section = configuration.GetSection(ServiceEndpointOptions.SectionName);

        return new ServiceEndpointOptions
        {
            CatalogGrpcUrl = section["CatalogGrpcUrl"] ?? "http://localhost:5272",
            CatalogGrpcTimeoutSeconds = int.TryParse(section["CatalogGrpcTimeoutSeconds"], out var timeoutSeconds)
                ? timeoutSeconds
                : 3,
            InventoryGrpcUrl = section["InventoryGrpcUrl"] ?? "http://localhost:5273",
            InventoryGrpcTimeoutSeconds = int.TryParse(section["InventoryGrpcTimeoutSeconds"], out var inventoryTimeoutSeconds)
                ? inventoryTimeoutSeconds
                : 3,
            PaymentGrpcUrl = section["PaymentGrpcUrl"] ?? "http://localhost:5285",
            PaymentGrpcTimeoutSeconds = int.TryParse(section["PaymentGrpcTimeoutSeconds"], out var paymentTimeoutSeconds)
                ? paymentTimeoutSeconds
                : 3
        };
    }
}
