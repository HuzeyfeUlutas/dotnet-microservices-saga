using FluentValidation;
using Inventory.Application.Abstractions.Messaging;
using Inventory.Application.Common.Exceptions;
using Inventory.Application.Features.Reservations.ReleaseOrderStock;
using Inventory.Domain.Exceptions;
using Marketplace.Contracts.Inventory.V1;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Inventory.Infrastructure.Messaging.Consumers;

public sealed class ReleaseStockRequestedConsumer(
    ISender sender,
    IIntegrationEventPublisher integrationEventPublisher,
    ILogger<ReleaseStockRequestedConsumer> logger) : IConsumer<ReleaseStockRequested>
{
    public async Task Consume(ConsumeContext<ReleaseStockRequested> context)
    {
        var message = context.Message;

        logger.LogInformation(
            "Consuming stock release request event {EventId} for order {OrderId}",
            message.EventId,
            message.OrderId);

        try
        {
            await sender.Send(
                new ReleaseOrderStockCommand(
                    message.OrderId,
                    message.Items
                        .Select(item => new ReleaseOrderStockItem(item.ProductId, item.Sku))
                        .ToList()),
                context.CancellationToken);

            await integrationEventPublisher.PublishAsync(
                new StockReleased(
                    Guid.NewGuid(),
                    message.EventId,
                    message.OrderId,
                    DateTime.UtcNow),
                context.CancellationToken);
        }
        catch (Exception exception) when (IsExpectedFailure(exception))
        {
            logger.LogWarning(
                exception,
                "Stock release request event {EventId} failed for order {OrderId}",
                message.EventId,
                message.OrderId);

            await integrationEventPublisher.PublishAsync(
                new StockReleaseFailed(
                    Guid.NewGuid(),
                    message.EventId,
                    message.OrderId,
                    exception.Message,
                    DateTime.UtcNow),
                context.CancellationToken);
        }
    }

    private static bool IsExpectedFailure(Exception exception)
    {
        return exception is ValidationException or NotFoundException or ConflictException or DomainException;
    }
}
