using Marketplace.Contracts.Inventory.V1;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Order.Infrastructure.Messaging;
using Order.Persistence.Context;
using Order.Persistence.Sagas;

namespace Order.Infrastructure.Messaging.Consumers;

public sealed class StockCommitFailedConsumer(
    OrderDbContext context,
    IPublishEndpoint publishEndpoint,
    ILogger<StockCommitFailedConsumer> logger) : IConsumer<StockCommitFailed>
{
    public async Task Consume(ConsumeContext<StockCommitFailed> consumeContext)
    {
        var message = consumeContext.Message;
        var order = await context.Orders
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == message.OrderId, consumeContext.CancellationToken);
        var sagaState = await context.OrderCheckoutSagaStates
            .FirstOrDefaultAsync(x => x.OrderId == message.OrderId, consumeContext.CancellationToken);

        if (order is null || sagaState is null)
        {
            logger.LogWarning("Order or saga state {OrderId} was not found while handling stock commit failed event {EventId}", message.OrderId, message.EventId);
            return;
        }

        if (sagaState.LastProcessedEventId == message.EventId ||
            sagaState.CurrentState != OrderCheckoutSagaStatus.StockCommitRequested)
        {
            return;
        }

        await publishEndpoint.Publish(
            new ReleaseStockRequested(
                Guid.NewGuid(),
                message.OrderId,
                order.Lines.ToStockReservationItems(),
                DateTime.UtcNow),
            consumeContext.CancellationToken);

        sagaState.CurrentState = OrderCheckoutSagaStatus.StockReleaseRequestedAfterStockCommitFailure;
        sagaState.LastProcessedEventId = message.EventId;
        sagaState.FailureReason = message.FailureReason;
        sagaState.UpdatedAtUtc = DateTime.UtcNow;

        await context.SaveChangesAsync(consumeContext.CancellationToken);
    }
}
