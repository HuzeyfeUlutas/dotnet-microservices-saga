using Marketplace.Contracts.Payment.V1;
using MassTransit;
using Order.Persistence.Sagas;

namespace Order.Infrastructure.Messaging.Sagas.Activities;

public sealed class RequestPendingPaymentCancellationAfterTimeoutActivity(
    IPublishEndpoint publishEndpoint) : IStateMachineActivity<OrderCheckoutSagaState, PaymentTimeoutExpired>
{
    public async Task Execute(
        BehaviorContext<OrderCheckoutSagaState, PaymentTimeoutExpired> behaviorContext,
        IBehavior<OrderCheckoutSagaState, PaymentTimeoutExpired> next)
    {
        var message = behaviorContext.Message;
        const string failureReason = "Payment timeout expired.";

        await publishEndpoint.Publish(
            new CancelPendingPaymentRequested(
                Guid.NewGuid(),
                behaviorContext.Saga.PaymentId,
                message.OrderId,
                CreateIdempotencyKey("payment-cancel-pending", message.EventId),
                failureReason,
                DateTime.UtcNow),
            behaviorContext.CancellationToken);

        behaviorContext.Saga.LastProcessedEventId = message.EventId;
        behaviorContext.Saga.FailureReason = failureReason;
        behaviorContext.Saga.UpdatedAtUtc = DateTime.UtcNow;

        await next.Execute(behaviorContext);
    }

    public Task Faulted<TException>(
        BehaviorExceptionContext<OrderCheckoutSagaState, PaymentTimeoutExpired, TException> behaviorContext,
        IBehavior<OrderCheckoutSagaState, PaymentTimeoutExpired> next)
        where TException : Exception
    {
        return next.Faulted(behaviorContext);
    }

    public void Probe(ProbeContext context)
    {
        context.CreateScope("request-pending-payment-cancellation-after-timeout");
    }

    public void Accept(StateMachineVisitor visitor)
    {
        visitor.Visit(this);
    }

    private static string CreateIdempotencyKey(string operation, Guid eventId)
    {
        return $"{operation}:{eventId:N}";
    }
}
