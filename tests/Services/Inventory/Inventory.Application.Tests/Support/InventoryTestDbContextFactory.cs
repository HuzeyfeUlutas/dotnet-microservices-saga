using Inventory.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Tests.Support;

internal sealed class InventoryTestDbContextFactory
{
    private readonly string _databaseName = Guid.NewGuid().ToString("N");

    public InventoryDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseInMemoryDatabase(_databaseName)
            .Options;

        return new InventoryDbContext(options);
    }
}
