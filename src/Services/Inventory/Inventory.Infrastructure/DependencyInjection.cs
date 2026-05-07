using Inventory.Application.Abstractions.Observability;
using Inventory.Infrastructure.Observability;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Inventory.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IInventoryMetrics, InventoryMetrics>();
        services.AddInventoryTracing(configuration);
        services.AddInventoryHealthChecks(configuration);

        return services;
    }
}
