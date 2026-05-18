using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Order.Application.Abstractions.Messaging;
using Order.Application.Contracts.IntegrationEvents;
using Order.Application.Abstractions.Services;
using Order.Domain.Enums;
using Order.Persistence.Context;
using Order.Persistence.Sagas;
using Payment.Application.Contracts.IntegrationEvents;

namespace Order.Infrastructure.Messaging.Consumers;

public sealed class PaymentAuthorizationFailedConsumer(
    OrderDbContext context,
    IIntegrationEventPublisher integrationEventPublisher,
    IInventoryReservationClient inventoryReservationClient,
    ILogger<PaymentAuthorizationFailedConsumer> logger) : IConsumer<PaymentAuthorizationFailed>
{
    public async Task Consume(ConsumeContext<PaymentAuthorizationFailed> consumeContext)
    {
        var message = consumeContext.Message;

        logger.LogInformation(
            "Consuming payment authorization failed event {EventId} for order {OrderId} and payment {PaymentId}",
            message.EventId,
            message.OrderId,
            message.PaymentId);

        var order = await context.Orders
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == message.OrderId, consumeContext.CancellationToken);

        if (order is null)
        {
            logger.LogWarning("Order {OrderId} was not found while handling payment authorization failed event {EventId}", message.OrderId, message.EventId);
            return;
        }

        var sagaState = await GetOrCreateSagaStateAsync(order.Id, message.PaymentId, consumeContext.CancellationToken);

        if (sagaState.LastProcessedEventId == message.EventId ||
            sagaState.CurrentState is OrderCheckoutSagaStatus.PaymentFailed or OrderCheckoutSagaStatus.Completed or OrderCheckoutSagaStatus.CaptureRequested or OrderCheckoutSagaStatus.Failed)
        {
            return;
        }

        if (order.Status != OrderStatus.WaitingForPayment)
        {
            logger.LogInformation("Order {OrderId} is in state {OrderStatus}; skipping payment authorization failure handling.", order.Id, order.Status);
            return;
        }

        foreach (var line in order.Lines)
        {
            await inventoryReservationClient.ReleaseAsync(
                line.ProductId,
                line.Sku,
                order.Id,
                consumeContext.CancellationToken);
        }

        order.MarkPaymentAsFailed(message.FailureReason);

        sagaState.PaymentId = message.PaymentId;
        sagaState.CurrentState = OrderCheckoutSagaStatus.PaymentFailed;
        sagaState.LastProcessedEventId = message.EventId;
        sagaState.FailureReason = message.FailureReason;
        sagaState.CompletedAtUtc = DateTime.UtcNow;
        sagaState.UpdatedAtUtc = DateTime.UtcNow;

        await integrationEventPublisher.PublishAsync(
            new OrderPaymentFailed(
                Guid.NewGuid(),
                order.Id,
                order.BuyerId,
                message.PaymentId,
                order.TotalAmount,
                order.Currency,
                message.FailureReason,
                order.Lines.ToIntegrationItems(),
                DateTime.UtcNow),
            consumeContext.CancellationToken);

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
