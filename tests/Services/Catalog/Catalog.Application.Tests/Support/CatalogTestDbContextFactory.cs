using System;
using Catalog.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Application.Tests.Support;

internal sealed class CatalogTestDbContextFactory : IDisposable
{
    private readonly string _databaseName = Guid.NewGuid().ToString("N");

    public CatalogDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseInMemoryDatabase(_databaseName)
            .Options;

        return new CatalogDbContext(options);
    }

    public void Dispose()
    {
    }
}
