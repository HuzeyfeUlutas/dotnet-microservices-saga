using FluentValidation;
using Inventory.Application.Abstractions.Messaging;
using Inventory.Application.Common.Exceptions;
using Inventory.Application.Features.Reservations.ReverseCommittedOrderStock;
using Inventory.Domain.Exceptions;
using Marketplace.Contracts.Inventory.V1;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Inventory.Infrastructure.Messaging.Consumers;

public sealed class ReverseCommittedStockRequestedConsumer(
    ISender sender,
    IIntegrationEventPublisher integrationEventPublisher,
    ILogger<ReverseCommittedStockRequestedConsumer> logger) : IConsumer<ReverseCommittedStockRequested>
{
    public async Task Consume(ConsumeContext<ReverseCommittedStockRequested> context)
    {
        var message = context.Message;

        logger.LogInformation(
            "Consuming committed stock reverse request event {EventId} for order {OrderId}",
            message.EventId,
            message.OrderId);

        try
        {
            await sender.Send(
                new ReverseCommittedOrderStockCommand(
                    message.OrderId,
                    message.Items
                        .Select(item => new ReverseCommittedOrderStockItem(item.ProductId, item.Sku))
                        .ToList()),
                context.CancellationToken);

            await integrationEventPublisher.PublishAsync(
                new CommittedStockReversed(
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
                "Committed stock reverse request event {EventId} failed for order {OrderId}",
                message.EventId,
                message.OrderId);

            await integrationEventPublisher.PublishAsync(
                new CommittedStockReverseFailed(
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
