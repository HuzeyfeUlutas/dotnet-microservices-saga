using FluentAssertions;
using Inventory.Domain.Entities;
using Inventory.Domain.Enums;
using Inventory.Domain.Exceptions;
using Xunit;

namespace Inventory.Domain.Tests;

public class InventoryItemTests
{
    [Fact]
    public void Reserve_should_return_existing_pending_reservation_for_matching_retry()
    {
        var item = CreateItem();
        var expiresAtUtc = DateTime.UtcNow.AddMinutes(10);

        var firstReservation = item.Reserve(Guid.NewGuid(), 2, DateTime.UtcNow, expiresAtUtc);
        var retriedReservation = item.Reserve(firstReservation.OrderId, 2, DateTime.UtcNow, expiresAtUtc);

        retriedReservation.Id.Should().Be(firstReservation.Id);
        item.ReservedQuantity.Should().Be(2);
        item.Reservations.Should().HaveCount(1);
    }

    [Fact]
    public void Reserve_should_reject_retry_when_quantity_differs()
    {
        var item = CreateItem();
        var orderId = Guid.NewGuid();

        item.Reserve(orderId, 2, DateTime.UtcNow, null);

        var action = () => item.Reserve(orderId, 3, DateTime.UtcNow, null);

        action.Should().Throw<DomainException>()
            .WithMessage("Pending reservation quantity does not match the retry request.");
    }

    [Fact]
    public void CommitReservation_should_be_idempotent_for_confirmed_reservation()
    {
        var item = CreateItem();
        var orderId = Guid.NewGuid();

        item.Reserve(orderId, 2, DateTime.UtcNow, null);

        var firstCommit = item.CommitReservation(orderId, DateTime.UtcNow);
        var secondCommit = item.CommitReservation(orderId, DateTime.UtcNow);

        firstCommit.Should().BeTrue();
        secondCommit.Should().BeFalse();
        item.TotalQuantity.Should().Be(8);
        item.ReservedQuantity.Should().Be(0);
        item.Reservations.Single().Status.Should().Be(InventoryReservationStatus.Confirmed);
    }

    [Fact]
    public void ReleaseReservation_should_be_idempotent_for_released_reservation()
    {
        var item = CreateItem();
        var orderId = Guid.NewGuid();

        item.Reserve(orderId, 2, DateTime.UtcNow, null);

        var firstRelease = item.ReleaseReservation(orderId, DateTime.UtcNow);
        var secondRelease = item.ReleaseReservation(orderId, DateTime.UtcNow);

        firstRelease.Should().BeTrue();
        secondRelease.Should().BeFalse();
        item.TotalQuantity.Should().Be(10);
        item.ReservedQuantity.Should().Be(0);
        item.Reservations.Single().Status.Should().Be(InventoryReservationStatus.Released);
    }

    [Fact]
    public void CommitReservation_should_reject_released_reservation()
    {
        var item = CreateItem();
        var orderId = Guid.NewGuid();

        item.Reserve(orderId, 2, DateTime.UtcNow, null);
        item.ReleaseReservation(orderId, DateTime.UtcNow);

        var action = () => item.CommitReservation(orderId, DateTime.UtcNow);

        action.Should().Throw<DomainException>()
            .WithMessage("Released reservation cannot be committed.");
    }

    [Fact]
    public void ReleaseReservation_should_reject_confirmed_reservation()
    {
        var item = CreateItem();
        var orderId = Guid.NewGuid();

        item.Reserve(orderId, 2, DateTime.UtcNow, null);
        item.CommitReservation(orderId, DateTime.UtcNow);

        var action = () => item.ReleaseReservation(orderId, DateTime.UtcNow);

        action.Should().Throw<DomainException>()
            .WithMessage("Confirmed reservation cannot be released.");
    }

    private static InventoryItem CreateItem()
    {
        return new InventoryItem(Guid.NewGuid(), "SKU-001", 10);
    }
}
