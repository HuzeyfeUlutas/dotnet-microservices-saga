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

public sealed class PaymentCapturedConsumer(
    OrderDbContext context,
    IIntegrationEventPublisher integrationEventPublisher,
    ILogger<PaymentCapturedConsumer> logger) : IConsumer<PaymentCaptured>
{
    public async Task Consume(ConsumeContext<PaymentCaptured> consumeContext)
    {
        var message = consumeContext.Message;

        logger.LogInformation(
            "Consuming payment captured event {EventId} for order {OrderId} and payment {PaymentId}",
            message.EventId,
            message.OrderId,
            message.PaymentId);

        var order = await context.Orders
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == message.OrderId, consumeContext.CancellationToken);

        if (order is null)
        {
            logger.LogWarning("Order {OrderId} was not found while handling payment captured event {EventId}", message.OrderId, message.EventId);
            return;
        }

        var sagaState = await GetOrCreateSagaStateAsync(order.Id, message.PaymentId, consumeContext.CancellationToken);

        if (sagaState.LastProcessedEventId == message.EventId ||
            sagaState.CurrentState is OrderCheckoutSagaStatus.Completed or OrderCheckoutSagaStatus.PaymentFailed or OrderCheckoutSagaStatus.Failed)
        {
            return;
        }

        if (order.Status == OrderStatus.WaitingForPayment)
        {
            order.MarkAsConfirmed();
        }
        else if (order.Status != OrderStatus.Confirmed)
        {
            logger.LogInformation("Order {OrderId} is in state {OrderStatus}; skipping payment captured handling.", order.Id, order.Status);
            return;
        }

        sagaState.PaymentId = message.PaymentId;
        sagaState.CurrentState = OrderCheckoutSagaStatus.Completed;
        sagaState.LastProcessedEventId = message.EventId;
        sagaState.CompletedAtUtc = DateTime.UtcNow;
        sagaState.FailureReason = null;
        sagaState.UpdatedAtUtc = DateTime.UtcNow;

        await integrationEventPublisher.PublishAsync(
            new OrderConfirmed(
                Guid.NewGuid(),
                order.Id,
                order.BuyerId,
                message.PaymentId,
                order.TotalAmount,
                order.Currency,
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
