using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Notification.Application.Abstractions.Observability;
using Notification.Application.Common.Exceptions;
using Notification.Application.Features.Notifications.CreateNotification;
using Notification.Application.Tests.Support;
using Notification.Domain.Enums;
using NSubstitute;
using Xunit;

namespace Notification.Application.Tests.Features.Notifications;

public class CreateNotificationHandlerTests
{
    [Fact]
    public async Task Handle_should_create_notification_and_use_current_correlation_id()
    {
        using var factory = new NotificationTestDbContextFactory();
        await using var context = factory.CreateContext();
        var correlationAccessor = new FakeCorrelationContextAccessor { CorrelationId = "corr-123" };
        var metrics = Substitute.For<INotificationMetrics>();
        var handler = new CreateNotificationHandler(context, correlationAccessor, metrics);

        var notificationId = await handler.Handle(
            new CreateNotificationCommand(
                "OrderConfirmed",
                "user-1",
                "user@example.com",
                "Subject",
                "Body",
                Guid.NewGuid()),
            CancellationToken.None);

        var notification = await context.NotificationMessages.SingleAsync(x => x.Id == notificationId);
        notification.Channel.Should().Be(NotificationChannel.Email);
        notification.RecipientId.Should().Be("user-1");
        notification.CorrelationId.Should().Be("corr-123");
        notification.Status.Should().Be(NotificationMessageStatus.Pending);

        metrics.Received(1).RecordNotificationCreated();
    }

    [Fact]
    public async Task Handle_should_return_existing_notification_id_when_same_source_event_recipient_and_type_exists()
    {
        using var factory = new NotificationTestDbContextFactory();
        await using var context = factory.CreateContext();
        var sourceEventId = Guid.NewGuid();
        var existingNotification = new Notification.Domain.Entities.NotificationMessage(
            NotificationChannel.Email,
            "OrderConfirmed",
            "user-1",
            "user@example.com",
            "Subject",
            "Body",
            sourceEventId);
        context.NotificationMessages.Add(existingNotification);
        await context.SaveChangesAsync();

        var handler = new CreateNotificationHandler(
            context,
            new FakeCorrelationContextAccessor(),
            Substitute.For<INotificationMetrics>());

        var notificationId = await handler.Handle(
            new CreateNotificationCommand(
                "OrderConfirmed",
                "user-1",
                "user@example.com",
                "Subject 2",
                "Body 2",
                sourceEventId),
            CancellationToken.None);

        notificationId.Should().Be(existingNotification.Id);
        context.NotificationMessages.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_should_create_skipped_notification_when_preference_is_disabled()
    {
        using var factory = new NotificationTestDbContextFactory();
        await using var context = factory.CreateContext();
        var preference = new Notification.Domain.Entities.RecipientPreference(
            "user-1",
            NotificationChannel.Email,
            "OrderConfirmed",
            false);
        preference.Disable("user opted out");
        context.RecipientPreferences.Add(preference);
        await context.SaveChangesAsync();

        var metrics = Substitute.For<INotificationMetrics>();
        var handler = new CreateNotificationHandler(
            context,
            new FakeCorrelationContextAccessor { CorrelationId = "corr-123" },
            metrics);

        var notificationId = await handler.Handle(
            new CreateNotificationCommand(
                "OrderConfirmed",
                "user-1",
                "user@example.com",
                "Subject",
                "Body"),
            CancellationToken.None);

        var notification = await context.NotificationMessages.SingleAsync(x => x.Id == notificationId);
        notification.Status.Should().Be(NotificationMessageStatus.Skipped);
        notification.SkipReason.Should().Be("user opted out");

        metrics.Received(1).RecordNotificationCreated();
        metrics.Received(1).RecordNotificationSkipped();
    }
}
