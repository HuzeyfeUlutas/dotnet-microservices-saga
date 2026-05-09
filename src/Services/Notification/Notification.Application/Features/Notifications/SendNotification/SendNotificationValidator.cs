using FluentValidation;

namespace Notification.Application.Features.Notifications.SendNotification;

public class SendNotificationValidator : AbstractValidator<SendNotificationCommand>
{
    public SendNotificationValidator()
    {
        RuleFor(x => x.NotificationMessageId)
            .NotEmpty();
    }
}
