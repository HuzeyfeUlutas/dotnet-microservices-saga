using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Notification.Application.Abstractions.Email;
using Notification.Application.Abstractions.Observability;
using Notification.Application.Common.Exceptions;
using Notification.Application.Features.Notifications.SendNotification;
using Notification.Application.Tests.Support;
using Notification.Domain.Entities;
using Notification.Domain.Enums;
using NSubstitute;
using Xunit;

namespace Notification.Application.Tests.Features.Notifications;

public class SendNotificationHandlerTests
{
    [Fact]
    public async Task Handle_should_mark_notification_as_sent_when_provider_succeeds()
    {
        using var factory = new NotificationTestDbContextFactory();
        await using var context = factory.CreateContext();
        var notification = new NotificationMessage(
            NotificationChannel.Email,
            "OrderConfirmed",
            "user-1",
            "user@example.com",
            "Subject",
            "Body");
        context.NotificationMessages.Add(notification);
        await context.SaveChangesAsync();

        var emailSender = Substitute.For<IEmailSender>();
        emailSender.ProviderName.Returns("FakeSmtp");
        emailSender.SendAsync(notification.Recipient, notification.Subject, notification.Body, Arg.Any<CancellationToken>())
            .Returns(EmailSendResult.Success("FakeSmtp", "provider-msg-1"));

        var metrics = Substitute.For<INotificationMetrics>();
        var handler = new SendNotificationHandler(context, emailSender, metrics);

        var result = await handler.Handle(new SendNotificationCommand(notification.Id), CancellationToken.None);

        result.Succeeded.Should().BeTrue();

        var storedNotification = await context.NotificationMessages
            .Include(x => x.DeliveryAttempts)
            .SingleAsync(x => x.Id == notification.Id);
        storedNotification.Status.Should().Be(NotificationMessageStatus.Sent);
        storedNotification.DeliveryAttempts.Should().ContainSingle();
        storedNotification.DeliveryAttempts.Single().Status.Should().Be(NotificationDeliveryAttemptStatus.Succeeded);

        metrics.Received(1).RecordDeliveryAttemptStarted();
        metrics.Received(1).RecordNotificationSent();
        metrics.Received(1).RecordDeliveryAttemptSucceeded();
    }

    [Fact]
    public async Task Handle_should_mark_notification_as_failed_when_provider_fails()
    {
        using var factory = new NotificationTestDbContextFactory();
        await using var context = factory.CreateContext();
        var notification = new NotificationMessage(
            NotificationChannel.Email,
            "OrderConfirmed",
            "user-1",
            "user@example.com",
            "Subject",
            "Body");
        context.NotificationMessages.Add(notification);
        await context.SaveChangesAsync();

        var emailSender = Substitute.For<IEmailSender>();
        emailSender.ProviderName.Returns("FakeSmtp");
        emailSender.SendAsync(notification.Recipient, notification.Subject, notification.Body, Arg.Any<CancellationToken>())
            .Returns(EmailSendResult.Failure("FakeSmtp", "smtp rejected recipient"));

        var metrics = Substitute.For<INotificationMetrics>();
        var handler = new SendNotificationHandler(context, emailSender, metrics);

        var result = await handler.Handle(new SendNotificationCommand(notification.Id), CancellationToken.None);

        result.Succeeded.Should().BeFalse();

        var storedNotification = await context.NotificationMessages
            .Include(x => x.DeliveryAttempts)
            .SingleAsync(x => x.Id == notification.Id);
        storedNotification.Status.Should().Be(NotificationMessageStatus.Failed);
        storedNotification.FailureReason.Should().Be("smtp rejected recipient");
        storedNotification.DeliveryAttempts.Single().Status.Should().Be(NotificationDeliveryAttemptStatus.Failed);

        metrics.Received(1).RecordNotificationFailed();
        metrics.Received(1).RecordDeliveryAttemptFailed();
    }

    [Fact]
    public async Task Handle_should_throw_conflict_when_notification_is_scheduled_for_future()
    {
        using var factory = new NotificationTestDbContextFactory();
        await using var context = factory.CreateContext();
        var notification = new NotificationMessage(
            NotificationChannel.Email,
            "OrderConfirmed",
            "user-1",
            "user@example.com",
            "Subject",
            "Body",
            scheduledAtUtc: DateTime.UtcNow.AddMinutes(10));
        context.NotificationMessages.Add(notification);
        await context.SaveChangesAsync();

        var handler = new SendNotificationHandler(
            context,
            Substitute.For<IEmailSender>(),
            Substitute.For<INotificationMetrics>());

        var action = () => handler.Handle(new SendNotificationCommand(notification.Id), CancellationToken.None);

        await action.Should().ThrowAsync<ConflictException>()
            .WithMessage("Scheduled notification cannot be sent before its scheduled date.");
    }

    [Fact]
    public async Task Handle_should_return_success_without_new_attempt_when_notification_is_already_sent()
    {
        using var factory = new NotificationTestDbContextFactory();
        await using var context = factory.CreateContext();
        var notification = new NotificationMessage(
            NotificationChannel.Email,
            "OrderConfirmed",
            "user-1",
            "user@example.com",
            "Subject",
            "Body");
        notification.StartDeliveryAttempt("FakeSmtp");
        notification.MarkAsSent("provider-msg-1");
        context.NotificationMessages.Add(notification);
        context.NotificationDeliveryAttempts.AddRange(notification.DeliveryAttempts);
        await context.SaveChangesAsync();

        var emailSender = Substitute.For<IEmailSender>();
        emailSender.ProviderName.Returns("FakeSmtp");
        var metrics = Substitute.For<INotificationMetrics>();
        var handler = new SendNotificationHandler(context, emailSender, metrics);

        var result = await handler.Handle(new SendNotificationCommand(notification.Id), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.ProviderMessageId.Should().Be("provider-msg-1");
        await emailSender.DidNotReceive()
            .SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());

        var storedNotification = await context.NotificationMessages
            .Include(x => x.DeliveryAttempts)
            .SingleAsync(x => x.Id == notification.Id);
        storedNotification.DeliveryAttempts.Should().ContainSingle();
    }

    [Fact]
    public async Task Handle_should_skip_notification_when_preference_is_disabled()
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

        var notification = new NotificationMessage(
            NotificationChannel.Email,
            "OrderConfirmed",
            "user-1",
            "user@example.com",
            "Subject",
            "Body");
        context.NotificationMessages.Add(notification);
        await context.SaveChangesAsync();

        var emailSender = Substitute.For<IEmailSender>();
        emailSender.ProviderName.Returns("FakeSmtp");
        var metrics = Substitute.For<INotificationMetrics>();
        var handler = new SendNotificationHandler(context, emailSender, metrics);

        var result = await handler.Handle(new SendNotificationCommand(notification.Id), CancellationToken.None);

        result.Skipped.Should().BeTrue();
        result.ErrorMessage.Should().Be("user opted out");
        await emailSender.DidNotReceive()
            .SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());

        var storedNotification = await context.NotificationMessages.SingleAsync(x => x.Id == notification.Id);
        storedNotification.Status.Should().Be(NotificationMessageStatus.Skipped);
        storedNotification.SkipReason.Should().Be("user opted out");
        metrics.Received(1).RecordNotificationSkipped();
    }

}
