using Inventory.Application.Abstractions.Persistence;
using Inventory.Domain.Entities;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Persistence.Context;

public class InventoryDbContext(DbContextOptions<InventoryDbContext> options) : DbContext(options), IInventoryDbContext
{
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<InventoryReservation> InventoryReservations => Set<InventoryReservation>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(InventoryDbContext).Assembly);
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
        base.OnModelCreating(modelBuilder);
    }
}
