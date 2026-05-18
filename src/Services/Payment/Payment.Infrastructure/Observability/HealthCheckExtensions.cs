using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Payment.Infrastructure.Configuration;
using RabbitMQ.Client;

namespace Payment.Infrastructure.Observability;

public static class HealthCheckExtensions
{
    public static IServiceCollection AddPaymentHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        var rabbitMqOptions = BuildRabbitMqOptions(configuration);
        var paymentConnectionString = configuration.GetConnectionString("PaymentDatabase")
                                      ?? throw new InvalidOperationException(
                                          "Connection string 'PaymentDatabase' was not found.");

        services.AddHealthChecks()
            .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(), tags: ["live"])
            .AddNpgSql(
                paymentConnectionString,
                name: "postgresql",
                tags: ["ready"])
            .AddRabbitMQ(
                _ => CreateRabbitMqConnectionAsync(rabbitMqOptions),
                name: "rabbitmq",
                tags: ["ready"]);

        return services;
    }

    private static Task<IConnection> CreateRabbitMqConnectionAsync(RabbitMqOptions options)
    {
        var factory = new ConnectionFactory
        {
            Uri = new Uri(BuildRabbitMqUri(options))
        };

        return factory.CreateConnectionAsync();
    }

    private static string BuildRabbitMqUri(RabbitMqOptions options)
    {
        var virtualHost = string.IsNullOrWhiteSpace(options.VirtualHost) ? "/" : options.VirtualHost;
        var normalizedVirtualHost = virtualHost.StartsWith("/") ? virtualHost[1..] : virtualHost;

        return string.IsNullOrWhiteSpace(normalizedVirtualHost)
            ? $"amqp://{options.Username}:{options.Password}@{options.Host}:{options.Port}"
            : $"amqp://{options.Username}:{options.Password}@{options.Host}:{options.Port}/{normalizedVirtualHost}";
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
