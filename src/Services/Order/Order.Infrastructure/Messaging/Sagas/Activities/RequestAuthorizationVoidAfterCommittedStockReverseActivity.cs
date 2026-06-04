using Marketplace.Contracts.Inventory.V1;
using Marketplace.Contracts.Payment.V1;
using MassTransit;
using Order.Persistence.Sagas;

namespace Order.Infrastructure.Messaging.Sagas.Activities;

public sealed class RequestAuthorizationVoidAfterCommittedStockReverseActivity(
    IPublishEndpoint publishEndpoint) : IStateMachineActivity<OrderCheckoutSagaState, CommittedStockReversed>
{
    public async Task Execute(
        BehaviorContext<OrderCheckoutSagaState, CommittedStockReversed> behaviorContext,
        IBehavior<OrderCheckoutSagaState, CommittedStockReversed> next)
    {
        var message = behaviorContext.Message;

        await publishEndpoint.Publish(
            new VoidPaymentAuthorizationRequested(
                Guid.NewGuid(),
                behaviorContext.Saga.PaymentId,
                message.OrderId,
                CreateIdempotencyKey("payment-auth-void", message.EventId),
                behaviorContext.Saga.FailureReason,
                DateTime.UtcNow),
            behaviorContext.CancellationToken);

        behaviorContext.Saga.LastProcessedEventId = message.EventId;
        behaviorContext.Saga.UpdatedAtUtc = DateTime.UtcNow;

        await next.Execute(behaviorContext);
    }

    public Task Faulted<TException>(
        BehaviorExceptionContext<OrderCheckoutSagaState, CommittedStockReversed, TException> behaviorContext,
        IBehavior<OrderCheckoutSagaState, CommittedStockReversed> next)
        where TException : Exception
    {
        return next.Faulted(behaviorContext);
    }

    public void Probe(ProbeContext context)
    {
        context.CreateScope("request-authorization-void-after-committed-stock-reverse");
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
