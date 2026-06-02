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

public class CheckoutFailureActivityTests
{
    [Fact]
    public async Task Payment_authorization_failure_should_request_stock_release()
    {
        var factory = new OrderTestDbContextFactory();
        await using var dbContext = factory.CreateContext();
        var order = CreateOrder("idem-authorization-failed");
        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync();

        var sagaState = CreateSagaState(order.Id, Guid.NewGuid(), OrderCheckoutSagaStatus.WaitingForPayment);
        var message = new PaymentAuthorizationFailed(
            Guid.NewGuid(),
            sagaState.PaymentId,
            order.Id,
            order.TotalAmount,
            order.Currency,
            "3ds failed",
            DateTime.UtcNow);
        var behaviorContext = CreateBehaviorContext(sagaState, message);
        var next = Substitute.For<IBehavior<OrderCheckoutSagaState, PaymentAuthorizationFailed>>();
        var publishEndpoint = Substitute.For<IPublishEndpoint>();
        var activity = new RequestStockReleaseAfterPaymentFailureActivity(dbContext, publishEndpoint);

        await activity.Execute(behaviorContext, next);

        await publishEndpoint.Received(1).Publish(
            Arg.Is<ReleaseStockRequested>(request =>
                request.OrderId == order.Id &&
                request.Items.Count == 1),
            CancellationToken.None);
        sagaState.FailureReason.Should().Be("3ds failed");
        sagaState.LastProcessedEventId.Should().Be(message.EventId);
        await next.Received(1).Execute(behaviorContext);
    }

    [Fact]
    public async Task Stock_commit_failure_should_request_stock_release()
    {
        var factory = new OrderTestDbContextFactory();
        await using var dbContext = factory.CreateContext();
        var order = CreateOrder("idem-stock-commit-failed");
        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync();

        var sagaState = CreateSagaState(order.Id, Guid.NewGuid(), OrderCheckoutSagaStatus.StockCommitRequested);
        var message = new StockCommitFailed(Guid.NewGuid(), Guid.NewGuid(), order.Id, "commit failed", DateTime.UtcNow);
        var behaviorContext = CreateBehaviorContext(sagaState, message);
        var next = Substitute.For<IBehavior<OrderCheckoutSagaState, StockCommitFailed>>();
        var publishEndpoint = Substitute.For<IPublishEndpoint>();
        var activity = new RequestStockReleaseAfterStockCommitFailureActivity(dbContext, publishEndpoint);

        await activity.Execute(behaviorContext, next);

        await publishEndpoint.Received(1).Publish(
            Arg.Is<ReleaseStockRequested>(request =>
                request.OrderId == order.Id &&
                request.Items.Count == 1),
            CancellationToken.None);
        sagaState.FailureReason.Should().Be("commit failed");
        sagaState.LastProcessedEventId.Should().Be(message.EventId);
        await next.Received(1).Execute(behaviorContext);
    }

    [Fact]
    public async Task Stock_release_after_payment_failure_should_fail_order_payment()
    {
        var factory = new OrderTestDbContextFactory();
        await using var dbContext = factory.CreateContext();
        var order = CreateOrder("idem-stock-released-after-payment-failure");
        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync();

        var sagaState = CreateSagaState(
            order.Id,
            Guid.NewGuid(),
            OrderCheckoutSagaStatus.StockReleaseRequestedAfterPaymentFailure,
            "3ds failed");
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
                failed.FailureReason == "3ds failed"),
            CancellationToken.None);
        await next.Received(1).Execute(behaviorContext);
    }

    [Fact]
    public async Task Stock_release_after_stock_commit_failure_should_request_authorization_void()
    {
        var sagaState = CreateSagaState(
            Guid.NewGuid(),
            Guid.NewGuid(),
            OrderCheckoutSagaStatus.StockReleaseRequestedAfterStockCommitFailure,
            "commit failed");
        var message = new StockReleased(Guid.NewGuid(), Guid.NewGuid(), sagaState.OrderId, DateTime.UtcNow);
        var behaviorContext = CreateBehaviorContext(sagaState, message);
        var next = Substitute.For<IBehavior<OrderCheckoutSagaState, StockReleased>>();
        var publishEndpoint = Substitute.For<IPublishEndpoint>();
        var activity = new RequestAuthorizationVoidAfterStockReleaseActivity(publishEndpoint);

        await activity.Execute(behaviorContext, next);

        await publishEndpoint.Received(1).Publish(
            Arg.Is<VoidPaymentAuthorizationRequested>(request =>
                request.OrderId == sagaState.OrderId &&
                request.PaymentId == sagaState.PaymentId &&
                request.Reason == "commit failed"),
            CancellationToken.None);
        sagaState.LastProcessedEventId.Should().Be(message.EventId);
        await next.Received(1).Execute(behaviorContext);
    }

    [Fact]
    public async Task Authorization_void_after_stock_commit_failure_should_fail_order()
    {
        var factory = new OrderTestDbContextFactory();
        await using var dbContext = factory.CreateContext();
        var order = CreateOrder("idem-authorization-voided");
        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync();

        var sagaState = CreateSagaState(
            order.Id,
            Guid.NewGuid(),
            OrderCheckoutSagaStatus.AuthorizationVoidRequestedAfterStockCommitFailure,
            "commit failed");
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
                failed.FailureReason == "commit failed"),
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
