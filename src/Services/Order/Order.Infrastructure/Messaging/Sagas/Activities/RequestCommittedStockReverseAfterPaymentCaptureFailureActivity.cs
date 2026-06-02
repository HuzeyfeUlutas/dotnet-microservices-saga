using Marketplace.Contracts.Inventory.V1;
using Marketplace.Contracts.Payment.V1;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Order.Domain.Enums;
using Order.Persistence.Context;
using Order.Persistence.Sagas;

namespace Order.Infrastructure.Messaging.Sagas.Activities;

public sealed class RequestCommittedStockReverseAfterPaymentCaptureFailureActivity(
    OrderDbContext context,
    IPublishEndpoint publishEndpoint) : IStateMachineActivity<OrderCheckoutSagaState, PaymentCaptureFailed>
{
    public async Task Execute(
        BehaviorContext<OrderCheckoutSagaState, PaymentCaptureFailed> behaviorContext,
        IBehavior<OrderCheckoutSagaState, PaymentCaptureFailed> next)
    {
        var message = behaviorContext.Message;
        var order = await context.Orders
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == message.OrderId, behaviorContext.CancellationToken)
            ?? throw new InvalidOperationException($"Order '{message.OrderId}' was not found while reversing committed stock after payment capture failure.");

        if (order.Status != OrderStatus.WaitingForPayment)
        {
            throw new InvalidOperationException($"Order '{order.Id}' must be waiting for payment before committed stock can be reversed.");
        }

        await publishEndpoint.Publish(
            new ReverseCommittedStockRequested(
                Guid.NewGuid(),
                message.OrderId,
                order.Lines.ToStockReservationItems(),
                DateTime.UtcNow),
            behaviorContext.CancellationToken);

        behaviorContext.Saga.PaymentId = message.PaymentId;
        behaviorContext.Saga.LastProcessedEventId = message.EventId;
        behaviorContext.Saga.FailureReason = message.FailureReason;
        behaviorContext.Saga.UpdatedAtUtc = DateTime.UtcNow;

        await next.Execute(behaviorContext);
    }

    public Task Faulted<TException>(
        BehaviorExceptionContext<OrderCheckoutSagaState, PaymentCaptureFailed, TException> behaviorContext,
        IBehavior<OrderCheckoutSagaState, PaymentCaptureFailed> next)
        where TException : Exception
    {
        return next.Faulted(behaviorContext);
    }

    public void Probe(ProbeContext context)
    {
        context.CreateScope("request-committed-stock-reverse-after-payment-capture-failure");
    }

    public void Accept(StateMachineVisitor visitor)
    {
        visitor.Visit(this);
    }
}
