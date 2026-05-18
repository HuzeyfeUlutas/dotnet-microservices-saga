using FluentAssertions;
using Notification.Domain.Entities;
using Notification.Domain.Enums;
using Notification.Domain.Exceptions;
using Xunit;

namespace Notification.Domain.Tests;

public class NotificationMessageTests
{
    [Fact]
    public void Constructor_should_initialize_pending_notification()
    {
        var scheduledAtUtc = DateTime.UtcNow.AddMinutes(5);

        var notification = new NotificationMessage(
            NotificationChannel.Email,
            "  OrderConfirmed  ",
            "  user-1  ",
            "  user@example.com  ",
            "  Subject  ",
            "  Body  ",
            Guid.NewGuid(),
            "  correlation-id  ",
            scheduledAtUtc);

        notification.Channel.Should().Be(NotificationChannel.Email);
        notification.NotificationType.Should().Be("OrderConfirmed");
        notification.RecipientId.Should().Be("user-1");
        notification.Recipient.Should().Be("user@example.com");
        notification.Subject.Should().Be("Subject");
        notification.Body.Should().Be("Body");
        notification.Status.Should().Be(NotificationMessageStatus.Pending);
        notification.CorrelationId.Should().Be("correlation-id");
        notification.ScheduledAtUtc.Should().Be(scheduledAtUtc);
    }

    [Fact]
    public void StartDeliveryAttempt_should_move_notification_to_processing()
    {
        var notification = CreateNotification();

        var attempt = notification.StartDeliveryAttempt("smtp");

        notification.Status.Should().Be(NotificationMessageStatus.Processing);
        notification.DeliveryAttempts.Should().ContainSingle();
        attempt.AttemptNumber.Should().Be(1);
        attempt.Provider.Should().Be("smtp");
        attempt.Status.Should().Be(NotificationDeliveryAttemptStatus.Processing);
        notification.ConcurrencyVersion.Should().Be(1);
    }

    [Fact]
    public void MarkAsSent_should_complete_current_attempt()
    {
        var notification = CreateNotification();
        notification.StartDeliveryAttempt("smtp");

        notification.MarkAsSent("provider-message-id");

        notification.Status.Should().Be(NotificationMessageStatus.Sent);
        notification.SentAtUtc.Should().NotBeNull();
        notification.DeliveryAttempts.Should().ContainSingle();
        notification.DeliveryAttempts.Single().Status.Should().Be(NotificationDeliveryAttemptStatus.Succeeded);
        notification.DeliveryAttempts.Single().ProviderMessageId.Should().Be("provider-message-id");
    }

    [Fact]
    public void MarkAsFailed_should_complete_current_attempt_with_failure()
    {
        var notification = CreateNotification();
        notification.StartDeliveryAttempt("smtp");

        notification.MarkAsFailed("provider failure");

        notification.Status.Should().Be(NotificationMessageStatus.Failed);
        notification.FailureReason.Should().Be("provider failure");
        notification.DeliveryAttempts.Single().Status.Should().Be(NotificationDeliveryAttemptStatus.Failed);
        notification.DeliveryAttempts.Single().ErrorMessage.Should().Be("provider failure");
    }

    [Fact]
    public void StartDeliveryAttempt_should_reject_completed_notification()
    {
        var notification = CreateNotification();
        notification.Cancel("user requested");

        var action = () => notification.StartDeliveryAttempt("smtp");

        action.Should().Throw<DomainException>()
            .WithMessage("Delivery attempt cannot be started for a completed notification.");
    }

    [Fact]
    public void StartDeliveryAttempt_should_reject_processing_notification()
    {
        var notification = CreateNotification();
        notification.StartDeliveryAttempt("smtp");

        var action = () => notification.StartDeliveryAttempt("smtp");

        action.Should().Throw<DomainException>()
            .WithMessage("Notification is already being processed.");
    }

    private static NotificationMessage CreateNotification()
    {
        return new NotificationMessage(
            NotificationChannel.Email,
            "OrderConfirmed",
            "user-1",
            "user@example.com",
            "Subject",
            "Body");
    }
}
