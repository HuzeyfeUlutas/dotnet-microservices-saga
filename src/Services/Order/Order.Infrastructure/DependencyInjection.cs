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

namespace Order.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IIntegrationEventPublisher, MassTransitIntegrationEventPublisher>();

        services.AddHttpClient<ICatalogPurchaseInfoClient, CatalogPurchaseInfoClient>((provider, client) =>
        {
            var options = BuildServiceEndpointOptions(configuration);
            client.BaseAddress = new Uri(options.CatalogBaseUrl);
        }).AddHttpMessageHandler(provider => new CorrelationPropagationHandler(provider.GetRequiredService<ICorrelationContextAccessor>()));

        services.AddHttpClient<IInventoryReservationClient, InventoryReservationClient>((provider, client) =>
        {
            var options = BuildServiceEndpointOptions(configuration);
            client.BaseAddress = new Uri(options.InventoryBaseUrl);
        }).AddHttpMessageHandler(provider => new CorrelationPropagationHandler(provider.GetRequiredService<ICorrelationContextAccessor>()));

        services.AddHttpClient<IPaymentClient, PaymentClient>((provider, client) =>
        {
            var options = BuildServiceEndpointOptions(configuration);
            client.BaseAddress = new Uri(options.PaymentBaseUrl);
        }).AddHttpMessageHandler(provider => new CorrelationPropagationHandler(provider.GetRequiredService<ICorrelationContextAccessor>()));

        services.AddMassTransit(x =>
        {
            x.SetKebabCaseEndpointNameFormatter();
            x.AddConsumer<PaymentAuthorizedConsumer>();
            x.AddConsumer<PaymentAuthorizationFailedConsumer>();
            x.AddConsumer<PaymentCapturedConsumer>();
            x.AddConsumer<PaymentCaptureFailedConsumer>();

            x.AddEntityFrameworkOutbox<OrderDbContext>(o =>
            {
                o.UsePostgres();
                o.UseBusOutbox();
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
            CatalogBaseUrl = section["CatalogBaseUrl"] ?? "http://localhost:5272",
            InventoryBaseUrl = section["InventoryBaseUrl"] ?? "http://localhost:5273",
            PaymentBaseUrl = section["PaymentBaseUrl"] ?? "http://localhost:5285"
        };
    }
}
