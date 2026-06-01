using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Order.Application.Abstractions.Messaging;
using Order.Application.Abstractions.Services;
using Order.Application.Common.Exceptions;
using Order.Application.Features.Checkout.CreateCheckout;
using Order.Application.Tests.Support;
using Order.Domain.ValueObjects;
using Xunit;

namespace Order.Application.Tests.Features.Checkout;

public class CreateCheckoutHandlerTests
{
    [Fact]
    public async Task Handle_should_reject_same_idempotency_key_when_buyer_is_different()
    {
        var factory = new OrderTestDbContextFactory();
        await using var context = factory.CreateContext();
        var existingOrder = new Order.Domain.Entities.Order(
            Guid.NewGuid(),
            "idem-conflict",
            [
                new OrderLineSnapshot(Guid.NewGuid(), "SKU-1", "Product 1", "Variant 1", new Money(100m, "TRY"), 1)
            ]);
        context.Orders.Add(existingOrder);
        await context.SaveChangesAsync();

        var catalogClient = Substitute.For<ICatalogPurchaseInfoClient>();
        var inventoryClient = Substitute.For<IInventoryReservationClient>();
        var paymentClient = Substitute.For<IPaymentClient>();
        var handler = new CreateCheckoutHandler(
            context,
            catalogClient,
            inventoryClient,
            paymentClient,
            Substitute.For<IStockReservationRollbackPublisher>(),
            NullLogger<CreateCheckoutHandler>.Instance);

        var act = () => handler.Handle(
            new CreateCheckoutCommand(
                Guid.NewGuid(),
                [new CreateCheckoutItem(existingOrder.Lines.Single().ProductId, "SKU-1", 1)],
                "idem-conflict"),
            CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("Checkout idempotency key 'idem-conflict' is already used for a different buyer.");
    }

    [Fact]
    public async Task Handle_should_attach_payment_to_existing_waiting_order_and_return_payment_action()
    {
        var factory = new OrderTestDbContextFactory();
        await using var context = factory.CreateContext();
        var existingOrder = new Order.Domain.Entities.Order(
            Guid.NewGuid(),
            "idem-existing",
            [
                new OrderLineSnapshot(Guid.Parse("11111111-1111-1111-1111-111111111111"), "SKU-1", "Product 1", "Variant 1", new Money(200m, "TRY"), 1)
            ]);
        context.Orders.Add(existingOrder);
        await context.SaveChangesAsync();

        var catalogClient = Substitute.For<ICatalogPurchaseInfoClient>();
        var inventoryClient = Substitute.For<IInventoryReservationClient>();
        var paymentClient = Substitute.For<IPaymentClient>();
        paymentClient.CreatePaymentAsync(
                existingOrder.Id,
                existingOrder.TotalAmount,
                existingOrder.Currency,
                existingOrder.IdempotencyKey,
                "Fake",
                "Card",
                CancellationToken.None)
            .Returns(new PaymentInitiationResultDto(
                Guid.Parse("22222222-2222-2222-2222-222222222222"),
                "RequiresAction",
                "Fake",
                new PaymentActionDto("Redirect", "/fake-3ds/payments/test")));

        var handler = new CreateCheckoutHandler(
            context,
            catalogClient,
            inventoryClient,
            paymentClient,
            Substitute.For<IStockReservationRollbackPublisher>(),
            NullLogger<CreateCheckoutHandler>.Instance);

        var result = await handler.Handle(
            new CreateCheckoutCommand(
                existingOrder.BuyerId,
                [new CreateCheckoutItem(Guid.Parse("11111111-1111-1111-1111-111111111111"), "SKU-1", 1)],
                "idem-existing"),
            CancellationToken.None);

        result.OrderId.Should().Be(existingOrder.Id);
        result.Payment.PaymentId.Should().Be(Guid.Parse("22222222-2222-2222-2222-222222222222"));
        result.Payment.Action.Type.Should().Be("Redirect");
        existingOrder.PaymentId.Should().Be(Guid.Parse("22222222-2222-2222-2222-222222222222"));
    }

    [Fact]
    public async Task Handle_should_create_order_for_new_checkout()
    {
        var factory = new OrderTestDbContextFactory();
        await using var context = factory.CreateContext();
        var productId = Guid.NewGuid();

        var catalogClient = Substitute.For<ICatalogPurchaseInfoClient>();
        catalogClient.GetPurchaseInfoAsync(productId, "SKU-1", CancellationToken.None)
            .Returns(new CatalogPurchaseInfoDto(
                productId,
                "Product 1",
                "SKU-1",
                "Variant 1",
                150m,
                "TRY",
                "Active",
                "Active",
                Guid.NewGuid(),
                Guid.NewGuid(),
                true,
                null));

        var inventoryClient = Substitute.For<IInventoryReservationClient>();
        inventoryClient.ReserveOrderStockAsync(
                Arg.Any<Guid>(),
                Arg.Any<IReadOnlyCollection<InventoryReservationItemDto>>(),
                Arg.Any<DateTime?>(),
                CancellationToken.None)
            .Returns(callInfo =>
            [
                new InventoryReservationResultDto(
                    Guid.NewGuid(),
                    productId,
                    "SKU-1",
                    callInfo.ArgAt<Guid>(0),
                    2,
                    "Pending",
                    callInfo.ArgAt<DateTime?>(2))
            ]);

        var paymentClient = Substitute.For<IPaymentClient>();
        paymentClient.CreatePaymentAsync(Arg.Any<Guid>(), 300m, "TRY", "idem-new", "Fake", "Card", CancellationToken.None)
            .Returns(new PaymentInitiationResultDto(
                Guid.NewGuid(),
                "RequiresAction",
                "Fake",
                new PaymentActionDto("Redirect", "/fake-3ds/payments/new")));

        var handler = new CreateCheckoutHandler(
            context,
            catalogClient,
            inventoryClient,
            paymentClient,
            Substitute.For<IStockReservationRollbackPublisher>(),
            NullLogger<CreateCheckoutHandler>.Instance);

        var result = await handler.Handle(
            new CreateCheckoutCommand(
                Guid.NewGuid(),
                [new CreateCheckoutItem(productId, "SKU-1", 2)],
                "idem-new"),
            CancellationToken.None);

        result.OrderStatus.Should().Be(Order.Domain.Enums.OrderStatus.WaitingForPayment);
        context.Orders.Should().ContainSingle(x => x.Id == result.OrderId);
        await inventoryClient.Received(1)
            .ReserveOrderStockAsync(
                result.OrderId,
                Arg.Is<IReadOnlyCollection<InventoryReservationItemDto>>(items =>
                    items.Count == 1 &&
                    items.Single().ProductId == productId &&
                    items.Single().Sku == "SKU-1" &&
                    items.Single().Quantity == 2),
                Arg.Any<DateTime?>(),
                CancellationToken.None);
    }
}
