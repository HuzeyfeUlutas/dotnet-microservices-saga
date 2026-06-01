using FluentAssertions;
using Inventory.Application.Abstractions.Observability;
using Inventory.Application.Features.Reservations.CommitOrderStock;
using Inventory.Application.Features.Reservations.ReleaseOrderStock;
using Inventory.Application.Tests.Support;
using Inventory.Domain.Entities;
using Inventory.Domain.Enums;
using Inventory.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

namespace Inventory.Application.Tests.Features.Reservations;

public class CommitAndReleaseOrderStockHandlerTests
{
    [Fact]
    public async Task CommitOrderStock_should_commit_all_reservations_atomically()
    {
        var factory = new InventoryTestDbContextFactory();
        var orderId = Guid.NewGuid();
        var firstProductId = Guid.NewGuid();
        var secondProductId = Guid.NewGuid();
        await SeedReservedInventoryAsync(factory, orderId, [
            new InventoryItem(firstProductId, "SKU-1", 10),
            new InventoryItem(secondProductId, "SKU-2", 8)
        ]);
        await using var context = factory.CreateContext();
        var metrics = Substitute.For<IInventoryMetrics>();
        var handler = new CommitOrderStockHandler(context, metrics);

        await handler.Handle(
            new CommitOrderStockCommand(
                orderId,
                [
                    new CommitOrderStockItem(firstProductId, "SKU-1"),
                    new CommitOrderStockItem(secondProductId, "SKU-2")
                ]),
            CancellationToken.None);

        var items = await context.InventoryItems.Include(item => item.Reservations).ToListAsync();
        items.Should().OnlyContain(item => item.ReservedQuantity == 0);
        items.SelectMany(item => item.Reservations).Should().OnlyContain(
            reservation => reservation.Status == InventoryReservationStatus.Confirmed);
        metrics.Received(2).RecordReservationCommitted();
    }

    [Fact]
    public async Task CommitOrderStock_should_not_persist_partial_commit_when_one_reservation_is_released()
    {
        var factory = new InventoryTestDbContextFactory();
        var orderId = Guid.NewGuid();
        var firstProductId = Guid.NewGuid();
        var secondProductId = Guid.NewGuid();
        var firstItem = new InventoryItem(firstProductId, "SKU-1", 10);
        var secondItem = new InventoryItem(secondProductId, "SKU-2", 8);
        firstItem.Reserve(orderId, 2, DateTime.UtcNow);
        secondItem.Reserve(orderId, 3, DateTime.UtcNow);
        secondItem.ReleaseReservation(orderId, DateTime.UtcNow);
        await SeedInventoryAsync(factory, [firstItem, secondItem]);

        await using (var context = factory.CreateContext())
        {
            var handler = new CommitOrderStockHandler(context, Substitute.For<IInventoryMetrics>());
            var action = async () => await handler.Handle(
                new CommitOrderStockCommand(
                    orderId,
                    [
                        new CommitOrderStockItem(firstProductId, "SKU-1"),
                        new CommitOrderStockItem(secondProductId, "SKU-2")
                    ]),
                CancellationToken.None);

            await action.Should().ThrowAsync<DomainException>();
        }

        await using var verificationContext = factory.CreateContext();
        var reservations = await verificationContext.InventoryReservations
            .OrderBy(reservation => reservation.Quantity)
            .ToListAsync();
        reservations[0].Status.Should().Be(InventoryReservationStatus.Pending);
        reservations[1].Status.Should().Be(InventoryReservationStatus.Released);
    }

    [Fact]
    public async Task ReleaseOrderStock_should_release_all_reservations_atomically()
    {
        var factory = new InventoryTestDbContextFactory();
        var orderId = Guid.NewGuid();
        var firstProductId = Guid.NewGuid();
        var secondProductId = Guid.NewGuid();
        await SeedReservedInventoryAsync(factory, orderId, [
            new InventoryItem(firstProductId, "SKU-1", 10),
            new InventoryItem(secondProductId, "SKU-2", 8)
        ]);
        await using var context = factory.CreateContext();
        var metrics = Substitute.For<IInventoryMetrics>();
        var handler = new ReleaseOrderStockHandler(context, metrics);

        await handler.Handle(
            new ReleaseOrderStockCommand(
                orderId,
                [
                    new ReleaseOrderStockItem(firstProductId, "SKU-1"),
                    new ReleaseOrderStockItem(secondProductId, "SKU-2")
                ]),
            CancellationToken.None);

        var items = await context.InventoryItems.Include(item => item.Reservations).ToListAsync();
        items.Should().OnlyContain(item => item.ReservedQuantity == 0);
        items.SelectMany(item => item.Reservations).Should().OnlyContain(
            reservation => reservation.Status == InventoryReservationStatus.Released);
        metrics.Received(2).RecordReservationReleased();
    }

    private static async Task SeedReservedInventoryAsync(
        InventoryTestDbContextFactory factory,
        Guid orderId,
        IReadOnlyCollection<InventoryItem> items)
    {
        foreach (var item in items)
        {
            item.Reserve(orderId, 2, DateTime.UtcNow);
        }

        await SeedInventoryAsync(factory, items);
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
