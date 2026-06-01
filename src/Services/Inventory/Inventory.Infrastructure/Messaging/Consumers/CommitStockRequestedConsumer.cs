using FluentValidation;
using Inventory.Application.Abstractions.Messaging;
using Inventory.Application.Common.Exceptions;
using Inventory.Application.Features.Reservations.CommitOrderStock;
using Inventory.Domain.Exceptions;
using Marketplace.Contracts.Inventory.V1;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Inventory.Infrastructure.Messaging.Consumers;

public sealed class CommitStockRequestedConsumer(
    ISender sender,
    IIntegrationEventPublisher integrationEventPublisher,
    ILogger<CommitStockRequestedConsumer> logger) : IConsumer<CommitStockRequested>
{
    public async Task Consume(ConsumeContext<CommitStockRequested> context)
    {
        var message = context.Message;

        logger.LogInformation(
            "Consuming stock commit request event {EventId} for order {OrderId}",
            message.EventId,
            message.OrderId);

        try
        {
            await sender.Send(
                new CommitOrderStockCommand(
                    message.OrderId,
                    message.Items
                        .Select(item => new CommitOrderStockItem(item.ProductId, item.Sku))
                        .ToList()),
                context.CancellationToken);

            await integrationEventPublisher.PublishAsync(
                new StockCommitted(
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
                "Stock commit request event {EventId} failed for order {OrderId}",
                message.EventId,
                message.OrderId);

            await integrationEventPublisher.PublishAsync(
                new StockCommitFailed(
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
