using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Order.Persistence.Context;

public class OrderDbContextFactory : IDesignTimeDbContextFactory<OrderDbContext>
{
    public OrderDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<OrderDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=orderdb;Username=postgres;Password=postgres");

        return new OrderDbContext(optionsBuilder.Options);
    }
}
