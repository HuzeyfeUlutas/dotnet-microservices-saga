using Marketplace.Contracts.Inventory.V1;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Order.Persistence.Context;
using Order.Persistence.Sagas;

namespace Order.Infrastructure.Messaging.Sagas.Activities;

public sealed class RequestStockReleaseAfterStockCommitFailureActivity(
    OrderDbContext context,
    IPublishEndpoint publishEndpoint) : IStateMachineActivity<OrderCheckoutSagaState, StockCommitFailed>
{
    public async Task Execute(
        BehaviorContext<OrderCheckoutSagaState, StockCommitFailed> behaviorContext,
        IBehavior<OrderCheckoutSagaState, StockCommitFailed> next)
    {
        var message = behaviorContext.Message;
        var order = await context.Orders
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == message.OrderId, behaviorContext.CancellationToken)
            ?? throw new InvalidOperationException($"Order '{message.OrderId}' was not found while releasing stock after stock commit failure.");

        await publishEndpoint.Publish(
            new ReleaseStockRequested(
                Guid.NewGuid(),
                message.OrderId,
                order.Lines.ToStockReservationItems(),
                DateTime.UtcNow),
            behaviorContext.CancellationToken);

        behaviorContext.Saga.LastProcessedEventId = message.EventId;
        behaviorContext.Saga.FailureReason = message.FailureReason;
        behaviorContext.Saga.UpdatedAtUtc = DateTime.UtcNow;

        await next.Execute(behaviorContext);
    }

    public Task Faulted<TException>(
        BehaviorExceptionContext<OrderCheckoutSagaState, StockCommitFailed, TException> behaviorContext,
        IBehavior<OrderCheckoutSagaState, StockCommitFailed> next)
        where TException : Exception
    {
        return next.Faulted(behaviorContext);
    }

    public void Probe(ProbeContext context)
    {
        context.CreateScope("request-stock-release-after-stock-commit-failure");
    }

    public void Accept(StateMachineVisitor visitor)
    {
        visitor.Visit(this);
    }
}
