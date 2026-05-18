using Microsoft.EntityFrameworkCore;
using Payment.Persistence.Context;

namespace Payment.Application.Tests.Support;

internal sealed class PaymentTestDbContextFactory
{
    public PaymentDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<PaymentDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new PaymentDbContext(options);
    }
}
