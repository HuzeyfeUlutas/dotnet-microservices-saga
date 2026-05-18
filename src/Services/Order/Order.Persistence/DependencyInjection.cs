using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Order.Application.Abstractions.Persistence;
using Order.Persistence.Context;

namespace Order.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("OrderDatabase")
                               ?? throw new InvalidOperationException("Connection string 'OrderDatabase' was not found.");

        services.AddDbContext<OrderDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IOrderDbContext>(provider => provider.GetRequiredService<OrderDbContext>());

        return services;
    }
}
