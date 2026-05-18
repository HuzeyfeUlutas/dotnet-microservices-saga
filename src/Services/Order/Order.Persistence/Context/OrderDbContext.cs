using MassTransit;
using Microsoft.EntityFrameworkCore;
using Order.Application.Abstractions.Persistence;
using Order.Domain.Entities;
using Order.Persistence.Sagas;
using OrderEntity = Order.Domain.Entities.Order;

namespace Order.Persistence.Context;

public class OrderDbContext(DbContextOptions<OrderDbContext> options) : DbContext(options), IOrderDbContext
{
    public DbSet<OrderEntity> Orders => Set<OrderEntity>();
    public DbSet<OrderLine> OrderLines => Set<OrderLine>();
    public DbSet<OrderCheckoutSagaState> OrderCheckoutSagaStates => Set<OrderCheckoutSagaState>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrderDbContext).Assembly);
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
        base.OnModelCreating(modelBuilder);
    }
}
