using FluentAssertions;
using Inventory.Application.Abstractions.Observability;
using Inventory.Application.Features.Reservations.ReserveOrderStock;
using Inventory.Application.Tests.Support;
using Inventory.Domain.Entities;
using Inventory.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

namespace Inventory.Application.Tests.Features.Reservations;

public class ReserveOrderStockHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReserveAllItemsWithOnePersistenceBoundary()
    {
        var factory = new InventoryTestDbContextFactory();
        var firstProductId = Guid.NewGuid();
        var secondProductId = Guid.NewGuid();
        await SeedInventoryAsync(factory, [
            new InventoryItem(firstProductId, "SKU-1", 10),
            new InventoryItem(secondProductId, "SKU-2", 8)
        ]);
        await using var context = factory.CreateContext();
        var metrics = Substitute.For<IInventoryMetrics>();
        var handler = new ReserveOrderStockHandler(context, metrics);
        var orderId = Guid.NewGuid();
        var expiresAtUtc = DateTime.UtcNow.AddMinutes(15);

        var result = await handler.Handle(
            new ReserveOrderStockCommand(
                orderId,
                [
                    new ReserveOrderStockItem(firstProductId, "SKU-1", 2),
                    new ReserveOrderStockItem(secondProductId, "SKU-2", 3)
                ],
                expiresAtUtc),
            CancellationToken.None);

        result.Should().HaveCount(2);
        var items = await context.InventoryItems
            .Include(item => item.Reservations)
            .OrderBy(item => item.Sku)
            .ToListAsync();
        items[0].ReservedQuantity.Should().Be(2);
        items[1].ReservedQuantity.Should().Be(3);
        items.SelectMany(item => item.Reservations).Should().HaveCount(2);
        metrics.Received(2).RecordStockReserved();
    }

    [Fact]
    public async Task Handle_ShouldNotPersistAnyReservation_WhenOneItemCannotBeReserved()
    {
        var factory = new InventoryTestDbContextFactory();
        var firstProductId = Guid.NewGuid();
        var secondProductId = Guid.NewGuid();
        await SeedInventoryAsync(factory, [
            new InventoryItem(firstProductId, "SKU-1", 10),
            new InventoryItem(secondProductId, "SKU-2", 1)
        ]);

        await using (var context = factory.CreateContext())
        {
            var handler = new ReserveOrderStockHandler(context, Substitute.For<IInventoryMetrics>());

            var action = async () => await handler.Handle(
                new ReserveOrderStockCommand(
                    Guid.NewGuid(),
                    [
                        new ReserveOrderStockItem(firstProductId, "SKU-1", 2),
                        new ReserveOrderStockItem(secondProductId, "SKU-2", 3)
                    ],
                    DateTime.UtcNow.AddMinutes(15)),
                CancellationToken.None);

            await action.Should().ThrowAsync<DomainException>()
                .WithMessage("Insufficient stock.");
        }

        await using var verificationContext = factory.CreateContext();
        var persistedItems = await verificationContext.InventoryItems
            .Include(item => item.Reservations)
            .ToListAsync();
        persistedItems.Should().HaveCount(2);
        persistedItems.Should().OnlyContain(item => item.ReservedQuantity == 0);
        persistedItems.SelectMany(item => item.Reservations).Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldReusePendingReservations_WhenBatchIsRetried()
    {
        var factory = new InventoryTestDbContextFactory();
        var productId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var expiresAtUtc = DateTime.UtcNow.AddMinutes(15);
        await SeedInventoryAsync(factory, [new InventoryItem(productId, "SKU-1", 10)]);
        var command = new ReserveOrderStockCommand(
            orderId,
            [new ReserveOrderStockItem(productId, "SKU-1", 2)],
            expiresAtUtc);

        await using (var firstContext = factory.CreateContext())
        {
            var handler = new ReserveOrderStockHandler(firstContext, Substitute.For<IInventoryMetrics>());
            await handler.Handle(command, CancellationToken.None);
        }

        await using (var retryContext = factory.CreateContext())
        {
            var handler = new ReserveOrderStockHandler(retryContext, Substitute.For<IInventoryMetrics>());
            await handler.Handle(command, CancellationToken.None);
        }

        await using var verificationContext = factory.CreateContext();
        var item = await verificationContext.InventoryItems
            .Include(inventoryItem => inventoryItem.Reservations)
            .SingleAsync();
        item.ReservedQuantity.Should().Be(2);
        item.Reservations.Should().ContainSingle();
        (await verificationContext.StockMovements.CountAsync()).Should().Be(2);
    }

    private static async Task SeedInventoryAsync(
        InventoryTestDbContextFactory factory,
        IReadOnlyCollection<InventoryItem> items)
    {
        await using var context = factory.CreateContext();
        context.InventoryItems.AddRange(items);
        await context.SaveChangesAsync();
    }
}
