using FluentAssertions;
using Marketplace.Contracts.Inventory.V1;
using Marketplace.Contracts.Payment.V1;
using MassTransit;
using NSubstitute;
using Order.Application.Abstractions.Messaging;
using Order.Application.Contracts.IntegrationEvents;
using Order.Domain.Enums;
using Order.Domain.ValueObjects;
using Order.Infrastructure.Messaging.Sagas;
using Order.Infrastructure.Messaging.Sagas.Activities;
using Order.Infrastructure.Tests.Support;
using Order.Persistence.Sagas;
using Xunit;

namespace Order.Infrastructure.Tests.Messaging.Sagas;

public class CheckoutTimeoutActivityTests
{
    [Fact]
    public async Task Payment_timeout_should_request_pending_payment_cancellation()
    {
        var sagaState = CreateSagaState(Guid.NewGuid(), Guid.NewGuid(), OrderCheckoutSagaStatus.WaitingForPayment);
        var message = new PaymentTimeoutExpired(Guid.NewGuid(), sagaState.OrderId, DateTime.UtcNow);
        var behaviorContext = CreateBehaviorContext(sagaState, message);
        var next = Substitute.For<IBehavior<OrderCheckoutSagaState, PaymentTimeoutExpired>>();
        var publishEndpoint = Substitute.For<IPublishEndpoint>();
        var activity = new RequestPendingPaymentCancellationAfterTimeoutActivity(publishEndpoint);

        await activity.Execute(behaviorContext, next);

        await publishEndpoint.Received(1).Publish(
            Arg.Is<CancelPendingPaymentRequested>(request =>
                request.OrderId == sagaState.OrderId &&
                request.PaymentId == sagaState.PaymentId &&
                request.IdempotencyKey == $"payment-cancel-pending:{message.EventId:N}" &&
                request.Reason == "Payment timeout expired."),
            CancellationToken.None);
        sagaState.FailureReason.Should().Be("Payment timeout expired.");
        sagaState.LastProcessedEventId.Should().Be(message.EventId);
        await next.Received(1).Execute(behaviorContext);
    }

    [Fact]
    public async Task Payment_cancellation_should_request_stock_release()
    {
        var factory = new OrderTestDbContextFactory();
        await using var dbContext = factory.CreateContext();
        var order = CreateOrder("idem-payment-cancelled");
        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync();

        var sagaState = CreateSagaState(
            order.Id,
            Guid.NewGuid(),
            OrderCheckoutSagaStatus.PendingPaymentCancellationRequestedAfterTimeout,
            "Payment timeout expired.");
        var message = new PaymentCancelled(Guid.NewGuid(), Guid.NewGuid(), sagaState.PaymentId, order.Id, DateTime.UtcNow);
        var behaviorContext = CreateBehaviorContext(sagaState, message);
        var next = Substitute.For<IBehavior<OrderCheckoutSagaState, PaymentCancelled>>();
        var publishEndpoint = Substitute.For<IPublishEndpoint>();
        var activity = new RequestStockReleaseAfterPaymentCancellationActivity(dbContext, publishEndpoint);

        await activity.Execute(behaviorContext, next);

        await publishEndpoint.Received(1).Publish(
            Arg.Is<ReleaseStockRequested>(request =>
                request.OrderId == order.Id &&
                request.Items.Count == 1),
            CancellationToken.None);
        sagaState.LastProcessedEventId.Should().Be(message.EventId);
        await next.Received(1).Execute(behaviorContext);
    }

    [Fact]
    public async Task Stock_release_after_payment_timeout_should_fail_order_payment()
    {
        var factory = new OrderTestDbContextFactory();
        await using var dbContext = factory.CreateContext();
        var order = CreateOrder("idem-timeout-stock-released");
        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync();

        var sagaState = CreateSagaState(
            order.Id,
            Guid.NewGuid(),
            OrderCheckoutSagaStatus.StockReleaseRequestedAfterPaymentTimeout,
            "Payment timeout expired.");
        var message = new StockReleased(Guid.NewGuid(), Guid.NewGuid(), order.Id, DateTime.UtcNow);
        var behaviorContext = CreateBehaviorContext(sagaState, message);
        var next = Substitute.For<IBehavior<OrderCheckoutSagaState, StockReleased>>();
        var integrationEventPublisher = Substitute.For<IIntegrationEventPublisher>();
        var activity = new FailOrderPaymentAfterStockReleaseActivity(dbContext, integrationEventPublisher);

        await activity.Execute(behaviorContext, next);

        order.Status.Should().Be(OrderStatus.PaymentFailed);
        await integrationEventPublisher.Received(1).PublishAsync(
            Arg.Is<OrderPaymentFailed>(failed =>
                failed.OrderId == order.Id &&
                failed.PaymentId == sagaState.PaymentId &&
                failed.FailureReason == "Payment timeout expired."),
            CancellationToken.None);
        await next.Received(1).Execute(behaviorContext);
    }

    private static BehaviorContext<OrderCheckoutSagaState, TMessage> CreateBehaviorContext<TMessage>(
        OrderCheckoutSagaState sagaState,
        TMessage message)
        where TMessage : class
    {
        var behaviorContext = Substitute.For<BehaviorContext<OrderCheckoutSagaState, TMessage>>();
        behaviorContext.Saga.Returns(sagaState);
        behaviorContext.Message.Returns(message);
        behaviorContext.CancellationToken.Returns(CancellationToken.None);
        return behaviorContext;
    }

    private static Order.Domain.Entities.Order CreateOrder(string idempotencyKey)
    {
        return new Order.Domain.Entities.Order(
            Guid.NewGuid(),
            idempotencyKey,
            [
                new OrderLineSnapshot(Guid.NewGuid(), "SKU-1", "Product 1", "Variant 1", new Money(100m, "TRY"), 1)
            ]);
    }

    private static OrderCheckoutSagaState CreateSagaState(
        Guid orderId,
        Guid paymentId,
        string currentState,
        string? failureReason = null)
    {
        return new OrderCheckoutSagaState
        {
            CorrelationId = orderId,
            OrderId = orderId,
            PaymentId = paymentId,
            CurrentState = currentState,
            FailureReason = failureReason,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
    }
}
