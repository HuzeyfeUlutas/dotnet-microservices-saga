using Marketplace.Contracts.Inventory.V1;
using Marketplace.Contracts.Payment.V1;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Order.Domain.Enums;
using Order.Persistence.Context;
using Order.Persistence.Sagas;

namespace Order.Infrastructure.Messaging.Sagas.Activities;

public sealed class RequestStockReleaseAfterPaymentFailureActivity(
    OrderDbContext context,
    IPublishEndpoint publishEndpoint) : IStateMachineActivity<OrderCheckoutSagaState, PaymentAuthorizationFailed>
{
    public async Task Execute(
        BehaviorContext<OrderCheckoutSagaState, PaymentAuthorizationFailed> behaviorContext,
        IBehavior<OrderCheckoutSagaState, PaymentAuthorizationFailed> next)
    {
        var message = behaviorContext.Message;
        var order = await context.Orders
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == message.OrderId, behaviorContext.CancellationToken)
            ?? throw new InvalidOperationException($"Order '{message.OrderId}' was not found while releasing stock after payment failure.");

        if (order.Status != OrderStatus.WaitingForPayment)
        {
            throw new InvalidOperationException($"Order '{order.Id}' must be waiting for payment before stock can be released.");
        }

        await publishEndpoint.Publish(
            new ReleaseStockRequested(
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
        BehaviorExceptionContext<OrderCheckoutSagaState, PaymentAuthorizationFailed, TException> behaviorContext,
        IBehavior<OrderCheckoutSagaState, PaymentAuthorizationFailed> next)
        where TException : Exception
    {
        return next.Faulted(behaviorContext);
    }

    public void Probe(ProbeContext context)
    {
        context.CreateScope("request-stock-release-after-payment-failure");
    }

    public void Accept(StateMachineVisitor visitor)
    {
        visitor.Visit(this);
    }
}
