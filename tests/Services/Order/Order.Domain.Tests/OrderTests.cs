using FluentAssertions;
using Order.Domain.Entities;
using Order.Domain.Enums;
using Order.Domain.Exceptions;
using Order.Domain.ValueObjects;
using Xunit;

namespace Order.Domain.Tests;

public class OrderTests
{
    [Fact]
    public void Confirm_flow_should_move_order_to_confirmed()
    {
        var order = CreateOrder();

        order.AttachPayment(Guid.NewGuid());
        order.MarkAsConfirmed();

        order.Status.Should().Be(OrderStatus.Confirmed);
        order.ConfirmedAtUtc.Should().NotBeNull();
        order.FailureReason.Should().BeNull();
    }

    [Fact]
    public void Mark_payment_as_failed_should_move_order_to_payment_failed()
    {
        var order = CreateOrder();

        order.MarkPaymentAsFailed("3ds failed");

        order.Status.Should().Be(OrderStatus.PaymentFailed);
        order.PaymentFailedAtUtc.Should().NotBeNull();
        order.FailureReason.Should().Be("3ds failed");
    }

    [Fact]
    public void Constructor_should_reject_mixed_currency_lines()
    {
        var buyerId = Guid.NewGuid();
        var act = () => new Order.Domain.Entities.Order(
            buyerId,
            "idem-1",
            [
                new OrderLineSnapshot(Guid.NewGuid(), "SKU-1", "Product 1", "Variant 1", new Money(100m, "TRY"), 1),
                new OrderLineSnapshot(Guid.NewGuid(), "SKU-2", "Product 2", "Variant 2", new Money(50m, "USD"), 1)
            ]);

        act.Should().Throw<DomainException>()
            .WithMessage("All order lines must use the same currency.");
    }

    private static Order.Domain.Entities.Order CreateOrder()
    {
        return new Order.Domain.Entities.Order(
            Guid.NewGuid(),
            "order-idem-1",
            [
                new OrderLineSnapshot(
                    Guid.NewGuid(),
                    "SKU-1",
                    "Product 1",
                    "Variant 1",
                    new Money(125.50m, "TRY"),
                    2)
            ]);
    }
}
