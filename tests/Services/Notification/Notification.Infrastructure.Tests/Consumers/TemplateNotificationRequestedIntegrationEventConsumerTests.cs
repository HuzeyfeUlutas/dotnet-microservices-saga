using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Notification.Application.Contracts.IntegrationEvents.Notifications;
using Notification.Application.Features.Notifications.CreateNotificationFromTemplate;
using Notification.Infrastructure.Messaging.Consumers;
using Xunit;

namespace Notification.Infrastructure.Tests.Consumers;

public class TemplateNotificationRequestedIntegrationEventConsumerTests
{
    [Fact]
    public async Task Consume_should_map_message_to_create_notification_from_template_command()
    {
        var sender = Substitute.For<ISender>();
        var consumer = new TemplateNotificationRequestedIntegrationEventConsumer(
            sender,
            NullLogger<TemplateNotificationRequestedIntegrationEventConsumer>.Instance);
        var message = new TemplateNotificationRequestedIntegrationEvent(
            Guid.NewGuid(),
            "OrderConfirmed",
            "order-confirmed",
            "user-1",
            "user@example.com",
            new Dictionary<string, string> { ["OrderId"] = "ORD-123" },
            DateTime.UtcNow.AddMinutes(5),
            DateTime.UtcNow);
        var context = Substitute.For<ConsumeContext<TemplateNotificationRequestedIntegrationEvent>>();
        context.Message.Returns(message);
        context.CorrelationId.Returns(Guid.Parse("22222222-2222-2222-2222-222222222222"));
        context.CancellationToken.Returns(CancellationToken.None);

        await consumer.Consume(context);

        await sender.Received(1).Send(
            Arg.Is<CreateNotificationFromTemplateCommand>(command =>
                command.NotificationType == message.NotificationType &&
                command.TemplateKey == message.TemplateKey &&
                command.RecipientId == message.RecipientId &&
                command.Recipient == message.Recipient &&
                command.SourceEventId == message.EventId &&
                command.CorrelationId == "22222222-2222-2222-2222-222222222222" &&
                command.ScheduledAtUtc == message.ScheduledAtUtc &&
                command.Variables["OrderId"] == "ORD-123"),
            CancellationToken.None);
    }
}
