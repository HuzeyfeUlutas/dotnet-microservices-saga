using Inventory.Application.Abstractions.Persistence;
using Inventory.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Inventory.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("InventoryDatabase")
                               ?? throw new InvalidOperationException("Connection string 'InventoryDatabase' was not found.");

        services.AddDbContext<InventoryDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IInventoryDbContext>(provider => provider.GetRequiredService<InventoryDbContext>());

        return services;
    }
}
