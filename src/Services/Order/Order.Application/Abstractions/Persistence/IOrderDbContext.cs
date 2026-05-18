using Microsoft.EntityFrameworkCore;
using Order.Domain.Entities;
using OrderEntity = Order.Domain.Entities.Order;

namespace Order.Application.Abstractions.Persistence;

public interface IOrderDbContext
{
    DbSet<OrderEntity> Orders { get; }
    DbSet<OrderLine> OrderLines { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
