using Marketplace.Contracts.Inventory.V1;
using Marketplace.Contracts.Payment.V1;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Order.Persistence.Context;
using Order.Persistence.Sagas;

namespace Order.Infrastructure.Messaging.Consumers;

public sealed class StockCommittedConsumer(
    OrderDbContext context,
    IPublishEndpoint publishEndpoint,
    ILogger<StockCommittedConsumer> logger) : IConsumer<StockCommitted>
{
    public async Task Consume(ConsumeContext<StockCommitted> consumeContext)
    {
        var message = consumeContext.Message;
        var sagaState = await context.OrderCheckoutSagaStates
            .FirstOrDefaultAsync(x => x.OrderId == message.OrderId, consumeContext.CancellationToken);

        if (sagaState is null)
        {
            logger.LogWarning("Order saga state {OrderId} was not found while handling stock committed event {EventId}", message.OrderId, message.EventId);
            return;
        }

        if (sagaState.LastProcessedEventId == message.EventId ||
            sagaState.CurrentState != OrderCheckoutSagaStatus.StockCommitRequested)
        {
            return;
        }

        await publishEndpoint.Publish(
            new CapturePaymentRequested(
                Guid.NewGuid(),
                sagaState.PaymentId,
                message.OrderId,
                DateTime.UtcNow),
            consumeContext.CancellationToken);

        sagaState.CurrentState = OrderCheckoutSagaStatus.CaptureRequested;
        sagaState.LastProcessedEventId = message.EventId;
        sagaState.FailureReason = null;
        sagaState.UpdatedAtUtc = DateTime.UtcNow;

        await context.SaveChangesAsync(consumeContext.CancellationToken);
    }
}
