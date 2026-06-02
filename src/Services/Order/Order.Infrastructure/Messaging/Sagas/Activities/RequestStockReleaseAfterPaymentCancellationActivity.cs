using Marketplace.Contracts.Inventory.V1;
using Marketplace.Contracts.Payment.V1;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Order.Persistence.Context;
using Order.Persistence.Sagas;

namespace Order.Infrastructure.Messaging.Sagas.Activities;

public sealed class RequestStockReleaseAfterPaymentCancellationActivity(
    OrderDbContext context,
    IPublishEndpoint publishEndpoint) : IStateMachineActivity<OrderCheckoutSagaState, PaymentCancelled>
{
    public async Task Execute(
        BehaviorContext<OrderCheckoutSagaState, PaymentCancelled> behaviorContext,
        IBehavior<OrderCheckoutSagaState, PaymentCancelled> next)
    {
        var message = behaviorContext.Message;
        var order = await context.Orders
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == message.OrderId, behaviorContext.CancellationToken)
            ?? throw new InvalidOperationException($"Order '{message.OrderId}' was not found while releasing stock after payment cancellation.");

        await publishEndpoint.Publish(
            new ReleaseStockRequested(
                Guid.NewGuid(),
                message.OrderId,
                order.Lines.ToStockReservationItems(),
                DateTime.UtcNow),
            behaviorContext.CancellationToken);

        behaviorContext.Saga.LastProcessedEventId = message.EventId;
        behaviorContext.Saga.UpdatedAtUtc = DateTime.UtcNow;

        await next.Execute(behaviorContext);
    }

    public Task Faulted<TException>(
        BehaviorExceptionContext<OrderCheckoutSagaState, PaymentCancelled, TException> behaviorContext,
        IBehavior<OrderCheckoutSagaState, PaymentCancelled> next)
        where TException : Exception
    {
        return next.Faulted(behaviorContext);
    }

    public void Probe(ProbeContext context)
    {
        context.CreateScope("request-stock-release-after-payment-cancellation");
    }

    public void Accept(StateMachineVisitor visitor)
    {
        visitor.Visit(this);
    }
}
