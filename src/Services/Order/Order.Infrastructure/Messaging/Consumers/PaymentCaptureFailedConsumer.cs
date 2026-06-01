using MassTransit;
using Marketplace.Contracts.Payment.V1;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Marketplace.Contracts.Inventory.V1;
using Order.Domain.Enums;
using Order.Infrastructure.Messaging;
using Order.Persistence.Context;
using Order.Persistence.Sagas;

namespace Order.Infrastructure.Messaging.Consumers;

public sealed class PaymentCaptureFailedConsumer(
    OrderDbContext context,
    IPublishEndpoint publishEndpoint,
    ILogger<PaymentCaptureFailedConsumer> logger) : IConsumer<PaymentCaptureFailed>
{
    public async Task Consume(ConsumeContext<PaymentCaptureFailed> consumeContext)
    {
        var message = consumeContext.Message;

        logger.LogInformation(
            "Consuming payment capture failed event {EventId} for order {OrderId} and payment {PaymentId}",
            message.EventId,
            message.OrderId,
            message.PaymentId);

        var order = await context.Orders
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == message.OrderId, consumeContext.CancellationToken);

        if (order is null)
        {
            logger.LogWarning("Order {OrderId} was not found while handling payment capture failed event {EventId}", message.OrderId, message.EventId);
            return;
        }

        var sagaState = await GetOrCreateSagaStateAsync(order.Id, message.PaymentId, consumeContext.CancellationToken);

        if (sagaState.LastProcessedEventId == message.EventId ||
            sagaState.CurrentState is OrderCheckoutSagaStatus.Failed or OrderCheckoutSagaStatus.Completed or OrderCheckoutSagaStatus.PaymentFailed)
        {
            return;
        }

        if (order.Status == OrderStatus.Confirmed)
        {
            logger.LogInformation("Order {OrderId} is already confirmed; skipping payment capture failure handling.", order.Id);
            return;
        }

        await publishEndpoint.Publish(
            new ReverseCommittedStockRequested(
                Guid.NewGuid(),
                order.Id,
                order.Lines.ToStockReservationItems(),
                DateTime.UtcNow),
            consumeContext.CancellationToken);

        sagaState.PaymentId = message.PaymentId;
        sagaState.CurrentState = OrderCheckoutSagaStatus.StockReverseRequestedAfterPaymentCaptureFailure;
        sagaState.LastProcessedEventId = message.EventId;
        sagaState.FailureReason = message.FailureReason;
        sagaState.UpdatedAtUtc = DateTime.UtcNow;

        await context.SaveChangesAsync(consumeContext.CancellationToken);
    }

    private async Task<OrderCheckoutSagaState> GetOrCreateSagaStateAsync(
        Guid orderId,
        Guid paymentId,
        CancellationToken cancellationToken)
    {
        var sagaState = await context.OrderCheckoutSagaStates
            .FirstOrDefaultAsync(x => x.OrderId == orderId, cancellationToken);

        if (sagaState is not null)
        {
            return sagaState;
        }

        sagaState = new OrderCheckoutSagaState
        {
            CorrelationId = Guid.NewGuid(),
            OrderId = orderId,
            PaymentId = paymentId,
            CurrentState = OrderCheckoutSagaStatus.WaitingForPayment,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        context.OrderCheckoutSagaStates.Add(sagaState);
        return sagaState;
    }
}
