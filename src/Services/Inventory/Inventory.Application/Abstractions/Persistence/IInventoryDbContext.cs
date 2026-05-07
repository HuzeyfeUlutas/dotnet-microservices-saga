using Inventory.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Abstractions.Persistence;

public interface IInventoryDbContext
{
    DbSet<InventoryItem> InventoryItems { get; }
    DbSet<InventoryReservation> InventoryReservations { get; }
    DbSet<StockMovement> StockMovements { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
