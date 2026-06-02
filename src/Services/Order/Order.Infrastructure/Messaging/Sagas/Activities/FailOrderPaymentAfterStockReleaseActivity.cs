using Marketplace.Contracts.Inventory.V1;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Order.Application.Abstractions.Messaging;
using Order.Application.Contracts.IntegrationEvents;
using Order.Persistence.Context;
using Order.Persistence.Sagas;

namespace Order.Infrastructure.Messaging.Sagas.Activities;

public sealed class FailOrderPaymentAfterStockReleaseActivity(
    OrderDbContext context,
    IIntegrationEventPublisher integrationEventPublisher) : IStateMachineActivity<OrderCheckoutSagaState, StockReleased>
{
    public async Task Execute(
        BehaviorContext<OrderCheckoutSagaState, StockReleased> behaviorContext,
        IBehavior<OrderCheckoutSagaState, StockReleased> next)
    {
        var message = behaviorContext.Message;
        var order = await context.Orders
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == message.OrderId, behaviorContext.CancellationToken)
            ?? throw new InvalidOperationException($"Order '{message.OrderId}' was not found while finalizing payment failure.");
        var failureReason = behaviorContext.Saga.FailureReason ?? "Payment authorization failed.";

        order.MarkPaymentAsFailed(failureReason);

        behaviorContext.Saga.LastProcessedEventId = message.EventId;
        behaviorContext.Saga.CompletedAtUtc = DateTime.UtcNow;
        behaviorContext.Saga.UpdatedAtUtc = DateTime.UtcNow;

        await integrationEventPublisher.PublishAsync(
            new OrderPaymentFailed(
                Guid.NewGuid(),
                order.Id,
                order.BuyerId,
                behaviorContext.Saga.PaymentId,
                order.TotalAmount,
                order.Currency,
                failureReason,
                order.Lines.ToIntegrationItems(),
                DateTime.UtcNow),
            behaviorContext.CancellationToken);

        await next.Execute(behaviorContext);
    }

    public Task Faulted<TException>(
        BehaviorExceptionContext<OrderCheckoutSagaState, StockReleased, TException> behaviorContext,
        IBehavior<OrderCheckoutSagaState, StockReleased> next)
        where TException : Exception
    {
        return next.Faulted(behaviorContext);
    }

    public void Probe(ProbeContext context)
    {
        context.CreateScope("fail-order-payment-after-stock-release");
    }

    public void Accept(StateMachineVisitor visitor)
    {
        visitor.Visit(this);
    }
}
