using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Notification.Application.Common.Exceptions;
using Notification.Application.Features.Notifications.CreateNotificationFromTemplate;
using Notification.Application.Tests.Support;
using Notification.Domain.Entities;
using Notification.Domain.Enums;
using NSubstitute;
using Xunit;

namespace Notification.Application.Tests.Features.Notifications;

public class CreateNotificationFromTemplateHandlerTests
{
    [Fact]
    public async Task Handle_should_render_template_and_create_notification()
    {
        using var factory = new NotificationTestDbContextFactory();
        await using var context = factory.CreateContext();
        context.NotificationTemplates.Add(new NotificationTemplate(
            "order-confirmed",
            NotificationChannel.Email,
            "Order {{OrderId}} confirmed",
            "Hello {{CustomerName}}, your payment is complete."));
        await context.SaveChangesAsync();
        var sender = Substitute.For<MediatR.ISender>();
        var notificationId = Guid.NewGuid();
        sender.Send(
                Arg.Any<Notification.Application.Features.Notifications.CreateNotification.CreateNotificationCommand>(),
                Arg.Any<CancellationToken>())
            .Returns(notificationId);

        var handler = new CreateNotificationFromTemplateHandler(context, sender);

        var result = await handler.Handle(
            new CreateNotificationFromTemplateCommand(
                "OrderConfirmed",
                "order-confirmed",
                "user-1",
                "user@example.com",
                new Dictionary<string, string>
                {
                    ["OrderId"] = "ORD-123",
                    ["CustomerName"] = "Ada"
                },
                Guid.NewGuid(),
                "corr-123"),
            CancellationToken.None);

        result.Should().Be(notificationId);
        await sender.Received(1).Send(
            Arg.Is<Notification.Application.Features.Notifications.CreateNotification.CreateNotificationCommand>(command =>
                command.NotificationType == "OrderConfirmed" &&
                command.RecipientId == "user-1" &&
                command.Recipient == "user@example.com" &&
                command.Subject == "Order ORD-123 confirmed" &&
                command.Body == "Hello Ada, your payment is complete." &&
                command.CorrelationId == "corr-123"),
            CancellationToken.None);
    }

    [Fact]
    public async Task Handle_should_throw_not_found_when_template_is_missing()
    {
        using var factory = new NotificationTestDbContextFactory();
        await using var context = factory.CreateContext();
        var sender = Substitute.For<MediatR.ISender>();
        var handler = new CreateNotificationFromTemplateHandler(context, sender);

        var action = () => handler.Handle(
            new CreateNotificationFromTemplateCommand(
                "OrderConfirmed",
                "missing-template",
                "user-1",
                "user@example.com",
                new Dictionary<string, string>()),
            CancellationToken.None);

        await action.Should().ThrowAsync<NotFoundException>()
            .WithMessage("Active notification template 'missing-template' was not found.");
    }

    [Fact]
    public async Task Handle_should_throw_conflict_when_template_variables_are_missing()
    {
        using var factory = new NotificationTestDbContextFactory();
        await using var context = factory.CreateContext();
        context.NotificationTemplates.Add(new NotificationTemplate(
            "order-confirmed",
            NotificationChannel.Email,
            "Order {{OrderId}} confirmed",
            "Hello {{CustomerName}}, your payment is complete."));
        await context.SaveChangesAsync();

        var sender = Substitute.For<MediatR.ISender>();
        var handler = new CreateNotificationFromTemplateHandler(context, sender);

        var action = () => handler.Handle(
            new CreateNotificationFromTemplateCommand(
                "OrderConfirmed",
                "order-confirmed",
                "user-1",
                "user@example.com",
                new Dictionary<string, string> { ["OrderId"] = "ORD-123" }),
            CancellationToken.None);

        await action.Should().ThrowAsync<ConflictException>()
            .WithMessage("Notification template 'order-confirmed' body is missing variables: CustomerName.");
    }
}
