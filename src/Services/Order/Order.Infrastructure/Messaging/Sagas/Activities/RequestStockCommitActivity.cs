using Marketplace.Contracts.Inventory.V1;
using Marketplace.Contracts.Payment.V1;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Order.Domain.Enums;
using Order.Persistence.Context;
using Order.Persistence.Sagas;

namespace Order.Infrastructure.Messaging.Sagas.Activities;

public sealed class RequestStockCommitActivity(
    OrderDbContext context,
    IPublishEndpoint publishEndpoint) : IStateMachineActivity<OrderCheckoutSagaState, PaymentAuthorized>
{
    public async Task Execute(
        BehaviorContext<OrderCheckoutSagaState, PaymentAuthorized> behaviorContext,
        IBehavior<OrderCheckoutSagaState, PaymentAuthorized> next)
    {
        var message = behaviorContext.Message;
        var order = await context.Orders
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == message.OrderId, behaviorContext.CancellationToken)
            ?? throw new InvalidOperationException($"Order '{message.OrderId}' was not found while requesting stock commit.");

        if (order.Status != OrderStatus.WaitingForPayment)
        {
            throw new InvalidOperationException($"Order '{order.Id}' must be waiting for payment before stock can be committed.");
        }

        await publishEndpoint.Publish(
            new CommitStockRequested(
                Guid.NewGuid(),
                message.OrderId,
                order.Lines.ToStockReservationItems(),
                DateTime.UtcNow),
            behaviorContext.CancellationToken);

        behaviorContext.Saga.PaymentId = message.PaymentId;
        behaviorContext.Saga.LastProcessedEventId = message.EventId;
        behaviorContext.Saga.FailureReason = null;
        behaviorContext.Saga.UpdatedAtUtc = DateTime.UtcNow;

        await next.Execute(behaviorContext);
    }

    public Task Faulted<TException>(
        BehaviorExceptionContext<OrderCheckoutSagaState, PaymentAuthorized, TException> behaviorContext,
        IBehavior<OrderCheckoutSagaState, PaymentAuthorized> next)
        where TException : Exception
    {
        return next.Faulted(behaviorContext);
    }

    public void Probe(ProbeContext context)
    {
        context.CreateScope("request-stock-commit");
    }

    public void Accept(StateMachineVisitor visitor)
    {
        visitor.Visit(this);
    }
}
