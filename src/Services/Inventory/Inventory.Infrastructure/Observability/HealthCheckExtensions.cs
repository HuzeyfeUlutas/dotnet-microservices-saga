using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Inventory.Infrastructure.Observability;

public static class HealthCheckExtensions
{
    public static IServiceCollection AddInventoryHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        var inventoryConnectionString = configuration.GetConnectionString("InventoryDatabase")
                                      ?? throw new InvalidOperationException(
                                          "Connection string 'InventoryDatabase' was not found.");

        services.AddHealthChecks()
            .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(), tags: ["live"])
            .AddNpgSql(
                inventoryConnectionString,
                name: "postgresql",
                tags: ["ready"]);

        return services;
    }
}
