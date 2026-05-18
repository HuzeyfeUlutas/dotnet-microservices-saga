using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using Notification.Application.Contracts.IntegrationEvents.Notifications;
using Notification.Application.Features.Notifications.CreateNotificationFromTemplate;

namespace Notification.Infrastructure.Messaging.Consumers;

public sealed class TemplateNotificationRequestedIntegrationEventConsumer(
    ISender sender,
    ILogger<TemplateNotificationRequestedIntegrationEventConsumer> logger)
    : IConsumer<TemplateNotificationRequestedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<TemplateNotificationRequestedIntegrationEvent> context)
    {
        var message = context.Message;

        logger.LogInformation(
            "Consuming template notification request event {EventId} for recipient {RecipientId}, template {TemplateKey}",
            message.EventId,
            message.RecipientId,
            message.TemplateKey);

        await sender.Send(
            new CreateNotificationFromTemplateCommand(
                message.NotificationType,
                message.TemplateKey,
                message.RecipientId,
                message.Recipient,
                message.Variables,
                message.EventId,
                context.CorrelationId?.ToString(),
                message.ScheduledAtUtc),
            context.CancellationToken);
    }
}
