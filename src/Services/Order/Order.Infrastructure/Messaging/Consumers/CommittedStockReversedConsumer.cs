using Marketplace.Contracts.Inventory.V1;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Order.Persistence.Context;
using Order.Persistence.Sagas;

namespace Order.Infrastructure.Messaging.Consumers;

public sealed class CommittedStockReversedConsumer(
    OrderDbContext context,
    ILogger<CommittedStockReversedConsumer> logger) : IConsumer<CommittedStockReversed>
{
    public async Task Consume(ConsumeContext<CommittedStockReversed> consumeContext)
    {
        var message = consumeContext.Message;
        var sagaState = await context.OrderCheckoutSagaStates
            .FirstOrDefaultAsync(x => x.OrderId == message.OrderId, consumeContext.CancellationToken);

        if (sagaState is null)
        {
            logger.LogWarning("Order saga state {OrderId} was not found while handling committed stock reversed event {EventId}", message.OrderId, message.EventId);
            return;
        }

        if (sagaState.LastProcessedEventId == message.EventId ||
            sagaState.CurrentState != OrderCheckoutSagaStatus.StockReverseRequestedAfterPaymentCaptureFailure)
        {
            return;
        }

        sagaState.CurrentState = OrderCheckoutSagaStatus.StockReversedAfterPaymentCaptureFailure;
        sagaState.LastProcessedEventId = message.EventId;
        sagaState.UpdatedAtUtc = DateTime.UtcNow;

        await context.SaveChangesAsync(consumeContext.CancellationToken);
    }
}
