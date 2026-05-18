using Microsoft.EntityFrameworkCore;
using Order.Persistence.Context;

namespace Order.Application.Tests.Support;

internal sealed class OrderTestDbContextFactory
{
    public OrderDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new OrderDbContext(options);
    }
}
