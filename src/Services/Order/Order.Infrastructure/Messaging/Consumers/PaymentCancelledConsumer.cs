using Marketplace.Contracts.Inventory.V1;
using Marketplace.Contracts.Payment.V1;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Order.Infrastructure.Messaging;
using Order.Persistence.Context;
using Order.Persistence.Sagas;

namespace Order.Infrastructure.Messaging.Consumers;

public sealed class PaymentCancelledConsumer(
    OrderDbContext context,
    IPublishEndpoint publishEndpoint,
    ILogger<PaymentCancelledConsumer> logger) : IConsumer<PaymentCancelled>
{
    public async Task Consume(ConsumeContext<PaymentCancelled> consumeContext)
    {
        var message = consumeContext.Message;
        var order = await context.Orders
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == message.OrderId, consumeContext.CancellationToken);
        var sagaState = await context.OrderCheckoutSagaStates
            .FirstOrDefaultAsync(x => x.OrderId == message.OrderId, consumeContext.CancellationToken);

        if (order is null || sagaState is null)
        {
            logger.LogWarning("Order or saga state {OrderId} was not found while handling payment cancelled event {EventId}", message.OrderId, message.EventId);
            return;
        }

        if (sagaState.LastProcessedEventId == message.EventId ||
            sagaState.CurrentState != OrderCheckoutSagaStatus.PendingPaymentCancellationRequestedAfterTimeout)
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

        sagaState.CurrentState = OrderCheckoutSagaStatus.StockReleaseRequestedAfterPaymentTimeout;
        sagaState.LastProcessedEventId = message.EventId;
        sagaState.UpdatedAtUtc = DateTime.UtcNow;

        await context.SaveChangesAsync(consumeContext.CancellationToken);
    }
}
