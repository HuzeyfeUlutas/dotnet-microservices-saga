using FluentAssertions;
using Marketplace.Contracts.Inventory.V1;
using Marketplace.Contracts.Payment.V1;
using MassTransit;
using NSubstitute;
using Order.Application.Abstractions.Messaging;
using Order.Application.Contracts.IntegrationEvents;
using Order.Domain.Enums;
using Order.Domain.ValueObjects;
using Order.Infrastructure.Messaging.Sagas.Activities;
using Order.Infrastructure.Tests.Support;
using Order.Persistence.Sagas;
using Xunit;

namespace Order.Infrastructure.Tests.Messaging.Sagas;

public class CheckoutCaptureFailureActivityTests
{
    [Fact]
    public async Task Payment_capture_failure_should_request_committed_stock_reverse()
    {
        var factory = new OrderTestDbContextFactory();
        await using var dbContext = factory.CreateContext();
        var order = CreateOrder("idem-capture-failed");
        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync();

        var sagaState = CreateSagaState(order.Id, Guid.NewGuid(), OrderCheckoutSagaStatus.CaptureRequested);
        var message = new PaymentCaptureFailed(
            Guid.NewGuid(),
            sagaState.PaymentId,
            order.Id,
            order.TotalAmount,
            order.Currency,
            "capture failed",
            DateTime.UtcNow);
        var behaviorContext = CreateBehaviorContext(sagaState, message);
        var next = Substitute.For<IBehavior<OrderCheckoutSagaState, PaymentCaptureFailed>>();
        var publishEndpoint = Substitute.For<IPublishEndpoint>();
        var activity = new RequestCommittedStockReverseAfterPaymentCaptureFailureActivity(dbContext, publishEndpoint);

        await activity.Execute(behaviorContext, next);

        await publishEndpoint.Received(1).Publish(
            Arg.Is<ReverseCommittedStockRequested>(request =>
                request.OrderId == order.Id &&
                request.Items.Count == 1),
            CancellationToken.None);
        order.Status.Should().Be(OrderStatus.WaitingForPayment);
        sagaState.FailureReason.Should().Be("capture failed");
        sagaState.LastProcessedEventId.Should().Be(message.EventId);
        await next.Received(1).Execute(behaviorContext);
    }

    [Fact]
    public async Task Committed_stock_reverse_should_request_authorization_void()
    {
        var sagaState = CreateSagaState(
            Guid.NewGuid(),
            Guid.NewGuid(),
            OrderCheckoutSagaStatus.StockReverseRequestedAfterPaymentCaptureFailure,
            "capture failed");
        var message = new CommittedStockReversed(Guid.NewGuid(), Guid.NewGuid(), sagaState.OrderId, DateTime.UtcNow);
        var behaviorContext = CreateBehaviorContext(sagaState, message);
        var next = Substitute.For<IBehavior<OrderCheckoutSagaState, CommittedStockReversed>>();
        var publishEndpoint = Substitute.For<IPublishEndpoint>();
        var activity = new RequestAuthorizationVoidAfterCommittedStockReverseActivity(publishEndpoint);

        await activity.Execute(behaviorContext, next);

        await publishEndpoint.Received(1).Publish(
            Arg.Is<VoidPaymentAuthorizationRequested>(request =>
                request.OrderId == sagaState.OrderId &&
                request.PaymentId == sagaState.PaymentId &&
                request.IdempotencyKey == $"payment-auth-void:{message.EventId:N}" &&
                request.Reason == "capture failed"),
            CancellationToken.None);
        sagaState.LastProcessedEventId.Should().Be(message.EventId);
        await next.Received(1).Execute(behaviorContext);
    }

    [Fact]
    public async Task Authorization_void_after_payment_capture_failure_should_fail_order()
    {
        var factory = new OrderTestDbContextFactory();
        await using var dbContext = factory.CreateContext();
        var order = CreateOrder("idem-capture-authorization-voided");
        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync();

        var sagaState = CreateSagaState(
            order.Id,
            Guid.NewGuid(),
            OrderCheckoutSagaStatus.AuthorizationVoidRequestedAfterPaymentCaptureFailure,
            "capture failed");
        var message = new PaymentAuthorizationVoided(Guid.NewGuid(), Guid.NewGuid(), sagaState.PaymentId, order.Id, DateTime.UtcNow);
        var behaviorContext = CreateBehaviorContext(sagaState, message);
        var next = Substitute.For<IBehavior<OrderCheckoutSagaState, PaymentAuthorizationVoided>>();
        var integrationEventPublisher = Substitute.For<IIntegrationEventPublisher>();
        var activity = new FailOrderAfterAuthorizationVoidActivity(dbContext, integrationEventPublisher);

        await activity.Execute(behaviorContext, next);

        order.Status.Should().Be(OrderStatus.Failed);
        await integrationEventPublisher.Received(1).PublishAsync(
            Arg.Is<OrderFailed>(failed =>
                failed.OrderId == order.Id &&
                failed.PaymentId == sagaState.PaymentId &&
                failed.FailureReason == "capture failed"),
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
