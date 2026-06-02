using Marketplace.Contracts.Inventory.V1;
using Marketplace.Contracts.Payment.V1;
using MassTransit;
using Order.Persistence.Sagas;

namespace Order.Infrastructure.Messaging.Sagas.Activities;

public sealed class RequestAuthorizationVoidAfterStockReleaseActivity(
    IPublishEndpoint publishEndpoint) : IStateMachineActivity<OrderCheckoutSagaState, StockReleased>
{
    public async Task Execute(
        BehaviorContext<OrderCheckoutSagaState, StockReleased> behaviorContext,
        IBehavior<OrderCheckoutSagaState, StockReleased> next)
    {
        var message = behaviorContext.Message;

        await publishEndpoint.Publish(
            new VoidPaymentAuthorizationRequested(
                Guid.NewGuid(),
                behaviorContext.Saga.PaymentId,
                message.OrderId,
                behaviorContext.Saga.FailureReason,
                DateTime.UtcNow),
            behaviorContext.CancellationToken);

        behaviorContext.Saga.LastProcessedEventId = message.EventId;
        behaviorContext.Saga.UpdatedAtUtc = DateTime.UtcNow;

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
        context.CreateScope("request-authorization-void-after-stock-release");
    }

    public void Accept(StateMachineVisitor visitor)
    {
        visitor.Visit(this);
    }
}
