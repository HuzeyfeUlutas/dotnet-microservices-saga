using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MassTransit;
using Notification.Application.Abstractions.Email;
using Notification.Application.Abstractions.Messaging;
using Notification.Application.Abstractions.Observability;
using Notification.Infrastructure.BackgroundServices;
using Notification.Infrastructure.Configuration;
using Notification.Infrastructure.Email;
using Notification.Infrastructure.Messaging;
using Notification.Infrastructure.Messaging.Consumers;
using Notification.Infrastructure.Observability;
using Notification.Persistence.Context;

namespace Notification.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<EmailDeliveryOptions>(
            configuration.GetSection(EmailDeliveryOptions.SectionName));
        services.Configure<NotificationDeliveryOptions>(
            configuration.GetSection(NotificationDeliveryOptions.SectionName));

        services.AddScoped<IEmailSender, LoggingEmailSender>();
        services.AddScoped<IIntegrationEventPublisher, MassTransitIntegrationEventPublisher>();
        services.AddSingleton<INotificationMetrics, NotificationMetrics>();
        services.AddHostedService<NotificationDispatcherHostedService>();
        services.AddNotificationTracing(configuration);
        services.AddNotificationHealthChecks(configuration);

        services.AddMassTransit(x =>
        {
            x.SetKebabCaseEndpointNameFormatter();
            x.AddConsumer<NotificationRequestedIntegrationEventConsumer>();
            x.AddConsumer<TemplateNotificationRequestedIntegrationEventConsumer>();

            x.AddEntityFrameworkOutbox<NotificationDbContext>(o =>
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
