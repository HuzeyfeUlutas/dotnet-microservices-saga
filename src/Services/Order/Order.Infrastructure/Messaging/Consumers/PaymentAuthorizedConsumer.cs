using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Order.Application.Abstractions.Services;
using Order.Domain.Enums;
using Order.Persistence.Context;
using Order.Persistence.Sagas;
using Payment.Application.Contracts.IntegrationEvents;

namespace Order.Infrastructure.Messaging.Consumers;

public sealed class PaymentAuthorizedConsumer(
    OrderDbContext context,
    IInventoryReservationClient inventoryReservationClient,
    IPublishEndpoint publishEndpoint,
    ILogger<PaymentAuthorizedConsumer> logger) : IConsumer<PaymentAuthorized>
{
    public async Task Consume(ConsumeContext<PaymentAuthorized> consumeContext)
    {
        var message = consumeContext.Message;

        logger.LogInformation(
            "Consuming payment authorized event {EventId} for order {OrderId} and payment {PaymentId}",
            message.EventId,
            message.OrderId,
            message.PaymentId);

        var order = await context.Orders
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == message.OrderId, consumeContext.CancellationToken);

        if (order is null)
        {
            logger.LogWarning("Order {OrderId} was not found while handling payment authorized event {EventId}", message.OrderId, message.EventId);
            return;
        }

        var sagaState = await GetOrCreateSagaStateAsync(order.Id, message.PaymentId, consumeContext.CancellationToken);

        if (sagaState.LastProcessedEventId == message.EventId ||
            sagaState.CurrentState is OrderCheckoutSagaStatus.CaptureRequested or OrderCheckoutSagaStatus.Completed or OrderCheckoutSagaStatus.PaymentFailed or OrderCheckoutSagaStatus.Failed)
        {
            return;
        }

        if (order.Status != OrderStatus.WaitingForPayment)
        {
            logger.LogInformation("Order {OrderId} is in state {OrderStatus}; skipping payment authorized handling.", order.Id, order.Status);
            return;
        }

        foreach (var line in order.Lines)
        {
            await inventoryReservationClient.CommitAsync(
                line.ProductId,
                line.Sku,
                order.Id,
                consumeContext.CancellationToken);
        }

        await publishEndpoint.Publish(
            new CapturePaymentRequested(
                Guid.NewGuid(),
                message.PaymentId,
                message.OrderId,
                DateTime.UtcNow),
            consumeContext.CancellationToken);

        sagaState.PaymentId = message.PaymentId;
        sagaState.CurrentState = OrderCheckoutSagaStatus.CaptureRequested;
        sagaState.LastProcessedEventId = message.EventId;
        sagaState.FailureReason = null;
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
