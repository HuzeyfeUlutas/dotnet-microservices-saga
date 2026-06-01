using Marketplace.Contracts.Payment.V1;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Order.Persistence.Context;
using Order.Persistence.Sagas;

namespace Order.Infrastructure.Messaging.Consumers;

public sealed class PaymentAuthorizationVoidFailedConsumer(
    OrderDbContext context,
    ILogger<PaymentAuthorizationVoidFailedConsumer> logger) : IConsumer<PaymentAuthorizationVoidFailed>
{
    public async Task Consume(ConsumeContext<PaymentAuthorizationVoidFailed> consumeContext)
    {
        var message = consumeContext.Message;
        var sagaState = await context.OrderCheckoutSagaStates
            .FirstOrDefaultAsync(x => x.OrderId == message.OrderId, consumeContext.CancellationToken);

        if (sagaState is null)
        {
            logger.LogWarning("Order saga state {OrderId} was not found while handling payment authorization void failed event {EventId}", message.OrderId, message.EventId);
            return;
        }

        if (sagaState.LastProcessedEventId == message.EventId ||
            sagaState.CurrentState is not (
                OrderCheckoutSagaStatus.AuthorizationVoidRequestedAfterStockCommitFailure or
                OrderCheckoutSagaStatus.AuthorizationVoidRequestedAfterPaymentCaptureFailure))
        {
            return;
        }

        sagaState.CurrentState = OrderCheckoutSagaStatus.ManualReviewRequired;
        sagaState.LastProcessedEventId = message.EventId;
        sagaState.FailureReason = message.FailureReason;
        sagaState.UpdatedAtUtc = DateTime.UtcNow;

        await context.SaveChangesAsync(consumeContext.CancellationToken);
    }
}
