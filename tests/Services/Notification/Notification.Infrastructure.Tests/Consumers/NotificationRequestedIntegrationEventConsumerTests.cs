using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Notification.Application.Contracts.IntegrationEvents.Notifications;
using Notification.Application.Features.Notifications.CreateNotification;
using Notification.Infrastructure.Messaging.Consumers;
using Xunit;

namespace Notification.Infrastructure.Tests.Consumers;

public class NotificationRequestedIntegrationEventConsumerTests
{
    [Fact]
    public async Task Consume_should_map_message_to_create_notification_command()
    {
        var sender = Substitute.For<ISender>();
        var consumer = new NotificationRequestedIntegrationEventConsumer(
            sender,
            NullLogger<NotificationRequestedIntegrationEventConsumer>.Instance);
        var message = new NotificationRequestedIntegrationEvent(
            Guid.NewGuid(),
            "OrderConfirmed",
            "user-1",
            "user@example.com",
            "Subject",
            "Body",
            DateTime.UtcNow.AddMinutes(5),
            DateTime.UtcNow);
        var context = Substitute.For<ConsumeContext<NotificationRequestedIntegrationEvent>>();
        context.Message.Returns(message);
        context.CorrelationId.Returns(Guid.Parse("11111111-1111-1111-1111-111111111111"));
        context.CancellationToken.Returns(CancellationToken.None);

        await consumer.Consume(context);

        await sender.Received(1).Send(
            Arg.Is<CreateNotificationCommand>(command =>
                command.NotificationType == message.NotificationType &&
                command.RecipientId == message.RecipientId &&
                command.Recipient == message.Recipient &&
                command.Subject == message.Subject &&
                command.Body == message.Body &&
                command.SourceEventId == message.EventId &&
                command.CorrelationId == "11111111-1111-1111-1111-111111111111" &&
                command.ScheduledAtUtc == message.ScheduledAtUtc),
            CancellationToken.None);
    }
}
