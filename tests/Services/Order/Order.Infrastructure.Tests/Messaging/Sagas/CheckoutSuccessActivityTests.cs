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

public class CheckoutSuccessActivityTests
{
    [Fact]
    public async Task Request_stock_commit_should_publish_reserved_order_items()
    {
        var factory = new OrderTestDbContextFactory();
        await using var dbContext = factory.CreateContext();
        var order = CreateOrder("idem-state-machine-authorized");
        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync();

        var sagaState = CreateSagaState(order.Id, Guid.NewGuid());
        var message = new PaymentAuthorized(Guid.NewGuid(), sagaState.PaymentId, order.Id, order.TotalAmount, order.Currency, DateTime.UtcNow);
        var behaviorContext = CreateBehaviorContext(sagaState, message);
        var next = Substitute.For<IBehavior<OrderCheckoutSagaState, PaymentAuthorized>>();
        var publishEndpoint = Substitute.For<IPublishEndpoint>();
        var activity = new RequestStockCommitActivity(dbContext, publishEndpoint);

        await activity.Execute(behaviorContext, next);

        await publishEndpoint.Received(1)
            .Publish(
                Arg.Is<CommitStockRequested>(request =>
                    request.OrderId == order.Id &&
                    request.Items.Count == 1 &&
                    request.Items.Single().ProductId == order.Lines.Single().ProductId),
                CancellationToken.None);
        sagaState.LastProcessedEventId.Should().Be(message.EventId);
        await next.Received(1).Execute(behaviorContext);
    }

    [Fact]
    public async Task Request_payment_capture_should_publish_payment_command()
    {
        var sagaState = CreateSagaState(Guid.NewGuid(), Guid.NewGuid());
        var message = new StockCommitted(Guid.NewGuid(), Guid.NewGuid(), sagaState.OrderId, DateTime.UtcNow);
        var behaviorContext = CreateBehaviorContext(sagaState, message);
        var next = Substitute.For<IBehavior<OrderCheckoutSagaState, StockCommitted>>();
        var publishEndpoint = Substitute.For<IPublishEndpoint>();
        var activity = new RequestPaymentCaptureActivity(publishEndpoint);

        await activity.Execute(behaviorContext, next);

        await publishEndpoint.Received(1)
            .Publish(
                Arg.Is<CapturePaymentRequested>(request =>
                    request.OrderId == sagaState.OrderId &&
                    request.PaymentId == sagaState.PaymentId &&
                    request.IdempotencyKey == $"payment-capture:{message.EventId:N}"),
                CancellationToken.None);
        sagaState.LastProcessedEventId.Should().Be(message.EventId);
        await next.Received(1).Execute(behaviorContext);
    }

    [Fact]
    public async Task Confirm_order_should_update_aggregate_and_publish_order_confirmed()
    {
        var factory = new OrderTestDbContextFactory();
        await using var dbContext = factory.CreateContext();
        var order = CreateOrder("idem-state-machine-captured");
        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync();

        var sagaState = CreateSagaState(order.Id, Guid.NewGuid());
        var message = new PaymentCaptured(Guid.NewGuid(), sagaState.PaymentId, order.Id, order.TotalAmount, order.Currency, DateTime.UtcNow);
        var behaviorContext = CreateBehaviorContext(sagaState, message);
        var next = Substitute.For<IBehavior<OrderCheckoutSagaState, PaymentCaptured>>();
        var integrationEventPublisher = Substitute.For<IIntegrationEventPublisher>();
        var activity = new ConfirmOrderActivity(dbContext, integrationEventPublisher);

        await activity.Execute(behaviorContext, next);

        order.Status.Should().Be(OrderStatus.Confirmed);
        sagaState.CompletedAtUtc.Should().NotBeNull();
        await integrationEventPublisher.Received(1)
            .PublishAsync(
                Arg.Is<OrderConfirmed>(confirmed =>
                    confirmed.OrderId == order.Id &&
                    confirmed.PaymentId == sagaState.PaymentId),
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

    private static OrderCheckoutSagaState CreateSagaState(Guid orderId, Guid paymentId)
    {
        return new OrderCheckoutSagaState
        {
            CorrelationId = orderId,
            OrderId = orderId,
            PaymentId = paymentId,
            CurrentState = OrderCheckoutSagaStatus.WaitingForPayment,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
    }
}
