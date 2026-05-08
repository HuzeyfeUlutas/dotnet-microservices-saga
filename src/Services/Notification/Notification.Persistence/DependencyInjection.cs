using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Notification.Application.Abstractions.Persistence;
using Notification.Persistence.Context;

namespace Notification.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("NotificationDatabase")
                               ?? throw new InvalidOperationException("Connection string 'NotificationDatabase' was not found.");

        services.AddDbContext<NotificationDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<INotificationDbContext>(provider => provider.GetRequiredService<NotificationDbContext>());

        return services;
    }
}
