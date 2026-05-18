using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using Notification.Application.Contracts.IntegrationEvents.Notifications;
using Notification.Application.Features.Notifications.CreateNotification;

namespace Notification.Infrastructure.Messaging.Consumers;

public sealed class NotificationRequestedIntegrationEventConsumer(
    ISender sender,
    ILogger<NotificationRequestedIntegrationEventConsumer> logger)
    : IConsumer<NotificationRequestedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<NotificationRequestedIntegrationEvent> context)
    {
        var message = context.Message;

        logger.LogInformation(
            "Consuming notification request event {EventId} for recipient {RecipientId} and type {NotificationType}",
            message.EventId,
            message.RecipientId,
            message.NotificationType);

        await sender.Send(
            new CreateNotificationCommand(
                message.NotificationType,
                message.RecipientId,
                message.Recipient,
                message.Subject,
                message.Body,
                message.EventId,
                context.CorrelationId?.ToString(),
                message.ScheduledAtUtc),
            context.CancellationToken);
    }
}
