using Marketplace.Contracts.Inventory.V1;
using Marketplace.Contracts.Payment.V1;
using MassTransit;
using Order.Persistence.Sagas;

namespace Order.Infrastructure.Messaging.Sagas.Activities;

public sealed class RequestPaymentCaptureActivity(
    IPublishEndpoint publishEndpoint) : IStateMachineActivity<OrderCheckoutSagaState, StockCommitted>
{
    public async Task Execute(
        BehaviorContext<OrderCheckoutSagaState, StockCommitted> behaviorContext,
        IBehavior<OrderCheckoutSagaState, StockCommitted> next)
    {
        var message = behaviorContext.Message;

        await publishEndpoint.Publish(
            new CapturePaymentRequested(
                Guid.NewGuid(),
                behaviorContext.Saga.PaymentId,
                message.OrderId,
                DateTime.UtcNow),
            behaviorContext.CancellationToken);

        behaviorContext.Saga.LastProcessedEventId = message.EventId;
        behaviorContext.Saga.FailureReason = null;
        behaviorContext.Saga.UpdatedAtUtc = DateTime.UtcNow;

        await next.Execute(behaviorContext);
    }

    public Task Faulted<TException>(
        BehaviorExceptionContext<OrderCheckoutSagaState, StockCommitted, TException> behaviorContext,
        IBehavior<OrderCheckoutSagaState, StockCommitted> next)
        where TException : Exception
    {
        return next.Faulted(behaviorContext);
    }

    public void Probe(ProbeContext context)
    {
        context.CreateScope("request-payment-capture");
    }

    public void Accept(StateMachineVisitor visitor)
    {
        visitor.Visit(this);
    }
}
