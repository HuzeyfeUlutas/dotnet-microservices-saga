using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Order.Application.Abstractions.Messaging;
using Order.Application.Contracts.IntegrationEvents;
using Order.Domain.Enums;
using Order.Persistence.Context;
using Order.Persistence.Sagas;
using Payment.Application.Contracts.IntegrationEvents;

namespace Order.Infrastructure.Messaging.Consumers;

public sealed class PaymentCaptureFailedConsumer(
    OrderDbContext context,
    IIntegrationEventPublisher integrationEventPublisher,
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

        if (order.Status != OrderStatus.Failed)
        {
            order.MarkAsFailed(message.FailureReason);
        }

        sagaState.PaymentId = message.PaymentId;
        sagaState.CurrentState = OrderCheckoutSagaStatus.Failed;
        sagaState.LastProcessedEventId = message.EventId;
        sagaState.FailureReason = message.FailureReason;
        sagaState.CompletedAtUtc = DateTime.UtcNow;
        sagaState.UpdatedAtUtc = DateTime.UtcNow;

        await integrationEventPublisher.PublishAsync(
            new OrderFailed(
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
