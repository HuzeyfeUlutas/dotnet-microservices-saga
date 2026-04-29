using Catalog.Application.Abstractions.Messaging;
using Catalog.Application.Abstractions.Observability;
using Catalog.Infrastructure.Configuration;
using Catalog.Infrastructure.Messaging;
using Catalog.Infrastructure.Observability;
using Catalog.Persistence.Context;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Catalog.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IIntegrationEventPublisher, MassTransitIntegrationEventPublisher>();
        services.AddSingleton<ICatalogMetrics, CatalogMetrics>();
        services.AddCatalogTracing(configuration);
        services.AddCatalogHealthChecks(configuration);

        services.AddMassTransit(x =>
        {
            x.SetKebabCaseEndpointNameFormatter();

            x.AddEntityFrameworkOutbox<CatalogDbContext>(o =>
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
