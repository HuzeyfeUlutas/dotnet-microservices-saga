using FluentAssertions;
using Marketplace.Contracts.Inventory.V1;
using Marketplace.Contracts.Payment.V1;
using MassTransit;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Order.Application.Abstractions.Messaging;
using Order.Application.Contracts.IntegrationEvents;
using Order.Domain.Enums;
using Order.Domain.ValueObjects;
using Order.Infrastructure.Messaging.Consumers;
using Order.Infrastructure.Tests.Support;
using Order.Persistence.Sagas;
using Xunit;

namespace Order.Infrastructure.Tests.Consumers;

public class PaymentCompensationResultConsumerTests
{
    [Fact]
    public async Task AuthorizationVoided_should_fail_order_and_publish_order_failed()
    {
        var factory = new OrderTestDbContextFactory();
        await using var context = factory.CreateContext();
        var order = CreateOrder("idem-auth-voided");
        var paymentId = Guid.NewGuid();
        context.Orders.Add(order);
        context.OrderCheckoutSagaStates.Add(CreateSagaState(
            order.Id,
            paymentId,
            OrderCheckoutSagaStatus.AuthorizationVoidRequestedAfterPaymentCaptureFailure,
            "capture failed"));
        await context.SaveChangesAsync();
        var publisher = Substitute.For<IIntegrationEventPublisher>();
        var consumer = new PaymentAuthorizationVoidedConsumer(
            context,
            publisher,
            NullLogger<PaymentAuthorizationVoidedConsumer>.Instance);
        var consumeContext = Substitute.For<ConsumeContext<PaymentAuthorizationVoided>>();
        consumeContext.Message.Returns(
            new PaymentAuthorizationVoided(Guid.NewGuid(), Guid.NewGuid(), paymentId, order.Id, DateTime.UtcNow));
        consumeContext.CancellationToken.Returns(CancellationToken.None);

        await consumer.Consume(consumeContext);

        context.Orders.Single().Status.Should().Be(OrderStatus.Failed);
        context.Orders.Single().FailureReason.Should().Be("capture failed");
        context.OrderCheckoutSagaStates.Single().CurrentState.Should().Be(OrderCheckoutSagaStatus.Failed);
        await publisher.Received(1).PublishAsync(
            Arg.Is<OrderFailed>(message =>
                message.OrderId == order.Id &&
                message.PaymentId == paymentId &&
                message.FailureReason == "capture failed"),
            CancellationToken.None);
    }

    [Fact]
    public async Task AuthorizationVoidFailed_should_require_manual_review()
    {
        var factory = new OrderTestDbContextFactory();
        await using var context = factory.CreateContext();
        var orderId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();
        context.OrderCheckoutSagaStates.Add(CreateSagaState(
            orderId,
            paymentId,
            OrderCheckoutSagaStatus.AuthorizationVoidRequestedAfterPaymentCaptureFailure,
            "capture failed"));
        await context.SaveChangesAsync();
        var consumer = new PaymentAuthorizationVoidFailedConsumer(
            context,
            NullLogger<PaymentAuthorizationVoidFailedConsumer>.Instance);
        var consumeContext = Substitute.For<ConsumeContext<PaymentAuthorizationVoidFailed>>();
        consumeContext.Message.Returns(
            new PaymentAuthorizationVoidFailed(Guid.NewGuid(), Guid.NewGuid(), paymentId, orderId, "void failed", DateTime.UtcNow));
        consumeContext.CancellationToken.Returns(CancellationToken.None);

        await consumer.Consume(consumeContext);

        context.OrderCheckoutSagaStates.Single().CurrentState.Should().Be(OrderCheckoutSagaStatus.ManualReviewRequired);
        context.OrderCheckoutSagaStates.Single().FailureReason.Should().Be("void failed");
    }

    [Fact]
    public async Task PaymentCancelled_should_request_stock_release_after_timeout()
    {
        var factory = new OrderTestDbContextFactory();
        await using var context = factory.CreateContext();
        var order = CreateOrder("idem-payment-cancelled");
        var paymentId = Guid.NewGuid();
        context.Orders.Add(order);
        context.OrderCheckoutSagaStates.Add(CreateSagaState(
            order.Id,
            paymentId,
            OrderCheckoutSagaStatus.PendingPaymentCancellationRequestedAfterTimeout,
            "Payment timeout expired."));
        await context.SaveChangesAsync();
        var publishEndpoint = Substitute.For<IPublishEndpoint>();
        var consumer = new PaymentCancelledConsumer(
            context,
            publishEndpoint,
            NullLogger<PaymentCancelledConsumer>.Instance);
        var consumeContext = Substitute.For<ConsumeContext<PaymentCancelled>>();
        consumeContext.Message.Returns(
            new PaymentCancelled(Guid.NewGuid(), Guid.NewGuid(), paymentId, order.Id, DateTime.UtcNow));
        consumeContext.CancellationToken.Returns(CancellationToken.None);

        await consumer.Consume(consumeContext);

        await publishEndpoint.Received(1).Publish(
            Arg.Is<ReleaseStockRequested>(message =>
                message.OrderId == order.Id &&
                message.Items.Count == 1),
            CancellationToken.None);
        context.OrderCheckoutSagaStates.Single().CurrentState.Should().Be(OrderCheckoutSagaStatus.StockReleaseRequestedAfterPaymentTimeout);
    }

    [Fact]
    public async Task PaymentCancellationFailed_should_require_manual_review()
    {
        var factory = new OrderTestDbContextFactory();
        await using var context = factory.CreateContext();
        var orderId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();
        context.OrderCheckoutSagaStates.Add(CreateSagaState(
            orderId,
            paymentId,
            OrderCheckoutSagaStatus.PendingPaymentCancellationRequestedAfterTimeout,
            "Payment timeout expired."));
        await context.SaveChangesAsync();
        var consumer = new PaymentCancellationFailedConsumer(
            context,
            NullLogger<PaymentCancellationFailedConsumer>.Instance);
        var consumeContext = Substitute.For<ConsumeContext<PaymentCancellationFailed>>();
        consumeContext.Message.Returns(
            new PaymentCancellationFailed(Guid.NewGuid(), Guid.NewGuid(), paymentId, orderId, "cancel failed", DateTime.UtcNow));
        consumeContext.CancellationToken.Returns(CancellationToken.None);

        await consumer.Consume(consumeContext);

        context.OrderCheckoutSagaStates.Single().CurrentState.Should().Be(OrderCheckoutSagaStatus.ManualReviewRequired);
        context.OrderCheckoutSagaStates.Single().FailureReason.Should().Be("cancel failed");
    }

    [Fact]
    public async Task StockReleased_should_fail_payment_after_timeout_cancellation()
    {
        var factory = new OrderTestDbContextFactory();
        await using var context = factory.CreateContext();
        var order = CreateOrder("idem-timeout-stock-released");
        var paymentId = Guid.NewGuid();
        context.Orders.Add(order);
        context.OrderCheckoutSagaStates.Add(CreateSagaState(
            order.Id,
            paymentId,
            OrderCheckoutSagaStatus.StockReleaseRequestedAfterPaymentTimeout,
            "Payment timeout expired."));
        await context.SaveChangesAsync();
        var publisher = Substitute.For<IIntegrationEventPublisher>();
        var consumer = new StockReleasedConsumer(
            context,
            publisher,
            NullLogger<StockReleasedConsumer>.Instance);
        var consumeContext = Substitute.For<ConsumeContext<StockReleased>>();
        consumeContext.Message.Returns(
            new StockReleased(Guid.NewGuid(), Guid.NewGuid(), order.Id, DateTime.UtcNow));
        consumeContext.CancellationToken.Returns(CancellationToken.None);

        await consumer.Consume(consumeContext);

        context.Orders.Single().Status.Should().Be(OrderStatus.PaymentFailed);
        context.OrderCheckoutSagaStates.Single().CurrentState.Should().Be(OrderCheckoutSagaStatus.PaymentFailed);
        await publisher.Received(1).PublishAsync(
            Arg.Is<OrderPaymentFailed>(message =>
                message.OrderId == order.Id &&
                message.PaymentId == paymentId &&
                message.FailureReason == "Payment timeout expired."),
            CancellationToken.None);
    }

    private static Order.Domain.Entities.Order CreateOrder(string idempotencyKey)
    {
        return new Order.Domain.Entities.Order(
            Guid.NewGuid(),
            idempotencyKey,
            [new OrderLineSnapshot(Guid.NewGuid(), "SKU-1", "Product 1", "Variant 1", new Money(100m, "TRY"), 1)]);
    }

    private static OrderCheckoutSagaState CreateSagaState(
        Guid orderId,
        Guid paymentId,
        string currentState,
        string failureReason)
    {
        return new OrderCheckoutSagaState
        {
            CorrelationId = Guid.NewGuid(),
            OrderId = orderId,
            PaymentId = paymentId,
            CurrentState = currentState,
            FailureReason = failureReason,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
    }
}
