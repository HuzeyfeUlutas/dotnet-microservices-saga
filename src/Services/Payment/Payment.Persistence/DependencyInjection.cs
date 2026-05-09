using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Payment.Application.Abstractions.Persistence;
using Payment.Persistence.Context;

namespace Payment.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("PaymentDatabase")
                               ?? throw new InvalidOperationException("Connection string 'PaymentDatabase' was not found.");

        services.AddDbContext<PaymentDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IPaymentDbContext>(provider => provider.GetRequiredService<PaymentDbContext>());

        return services;
    }
}
