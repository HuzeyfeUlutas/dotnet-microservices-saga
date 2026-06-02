using Marketplace.Contracts.Inventory.V1;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Order.Application.Abstractions.Messaging;
using Order.Application.Contracts.IntegrationEvents;
using Order.Persistence.Context;
using Order.Persistence.Sagas;

namespace Order.Infrastructure.Messaging.Consumers;

public sealed class StockReleasedConsumer(
    OrderDbContext context,
    IIntegrationEventPublisher integrationEventPublisher,
    ILogger<StockReleasedConsumer> logger) : IConsumer<StockReleased>
{
    public async Task Consume(ConsumeContext<StockReleased> consumeContext)
    {
        var message = consumeContext.Message;
        var order = await context.Orders
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == message.OrderId, consumeContext.CancellationToken);
        var sagaState = await context.OrderCheckoutSagaStates
            .FirstOrDefaultAsync(x => x.OrderId == message.OrderId, consumeContext.CancellationToken);

        if (order is null || sagaState is null)
        {
            logger.LogInformation("Order or saga state {OrderId} was not found while handling stock released event {EventId}", message.OrderId, message.EventId);
            return;
        }

        if (sagaState.LastProcessedEventId == message.EventId)
        {
            return;
        }

        if (sagaState.CurrentState != OrderCheckoutSagaStatus.StockReleaseRequestedAfterPaymentTimeout)
        {
            return;
        }

        var failureReason = sagaState.FailureReason ?? "Payment authorization failed or timed out.";
        order.MarkPaymentAsFailed(failureReason);

        sagaState.CurrentState = OrderCheckoutSagaStatus.PaymentFailed;
        sagaState.LastProcessedEventId = message.EventId;
        sagaState.CompletedAtUtc = DateTime.UtcNow;
        sagaState.UpdatedAtUtc = DateTime.UtcNow;

        await integrationEventPublisher.PublishAsync(
            new OrderPaymentFailed(
                Guid.NewGuid(),
                order.Id,
                order.BuyerId,
                sagaState.PaymentId,
                order.TotalAmount,
                order.Currency,
                failureReason,
                order.Lines.ToIntegrationItems(),
                DateTime.UtcNow),
            consumeContext.CancellationToken);

        await context.SaveChangesAsync(consumeContext.CancellationToken);
    }
}
