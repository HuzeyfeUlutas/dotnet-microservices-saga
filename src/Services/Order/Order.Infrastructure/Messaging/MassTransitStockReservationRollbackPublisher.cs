using Marketplace.Contracts.Inventory.V1;
using MassTransit;
using Microsoft.Extensions.Logging;
using Order.Application.Abstractions.Messaging;
using Order.Application.Abstractions.Observability;

namespace Order.Infrastructure.Messaging;

public sealed class MassTransitStockReservationRollbackPublisher(
    IBus bus,
    ICorrelationContextAccessor correlationContextAccessor,
    ILogger<MassTransitStockReservationRollbackPublisher> logger) : IStockReservationRollbackPublisher
{
    public Task PublishReleaseImmediatelyAsync(
        Guid orderId,
        IReadOnlyCollection<StockReservationItem> items,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Publishing immediate stock release rollback request for order {OrderId}",
            orderId);

        return bus.Publish(
            new ReleaseStockRequested(
                Guid.NewGuid(),
                orderId,
                items,
                DateTime.UtcNow),
            publishContext =>
            {
                if (Guid.TryParse(correlationContextAccessor.CorrelationId, out var correlationId))
                {
                    publishContext.CorrelationId = correlationId;
                }

                if (!string.IsNullOrWhiteSpace(correlationContextAccessor.CorrelationId))
                {
                    publishContext.Headers.Set("X-Correlation-Id", correlationContextAccessor.CorrelationId);
                }
            },
            cancellationToken);
    }
}
