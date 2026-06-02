using Marketplace.Contracts.Payment.V1;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Order.Application.Abstractions.Messaging;
using Order.Application.Contracts.IntegrationEvents;
using Order.Domain.Enums;
using Order.Persistence.Context;
using Order.Persistence.Sagas;

namespace Order.Infrastructure.Messaging.Consumers;

public sealed class PaymentAuthorizationVoidedConsumer(
    OrderDbContext context,
    IIntegrationEventPublisher integrationEventPublisher,
    ILogger<PaymentAuthorizationVoidedConsumer> logger) : IConsumer<PaymentAuthorizationVoided>
{
    public async Task Consume(ConsumeContext<PaymentAuthorizationVoided> consumeContext)
    {
        var message = consumeContext.Message;
        var order = await context.Orders
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == message.OrderId, consumeContext.CancellationToken);
        var sagaState = await context.OrderCheckoutSagaStates
            .FirstOrDefaultAsync(x => x.OrderId == message.OrderId, consumeContext.CancellationToken);

        if (order is null || sagaState is null)
        {
            logger.LogWarning("Order or saga state {OrderId} was not found while handling payment authorization voided event {EventId}", message.OrderId, message.EventId);
            return;
        }

        if (sagaState.LastProcessedEventId == message.EventId ||
            sagaState.CurrentState != OrderCheckoutSagaStatus.AuthorizationVoidRequestedAfterPaymentCaptureFailure)
        {
            return;
        }

        var failureReason = sagaState.FailureReason ?? "Checkout compensation completed after payment processing failure.";

        if (order.Status != OrderStatus.Failed)
        {
            order.MarkAsFailed(failureReason);
        }

        sagaState.CurrentState = OrderCheckoutSagaStatus.Failed;
        sagaState.LastProcessedEventId = message.EventId;
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
                failureReason,
                order.Lines.ToIntegrationItems(),
                DateTime.UtcNow),
            consumeContext.CancellationToken);

        await context.SaveChangesAsync(consumeContext.CancellationToken);
    }
}
