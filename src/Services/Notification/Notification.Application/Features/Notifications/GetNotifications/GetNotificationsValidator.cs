using FluentValidation;

namespace Notification.Application.Features.Notifications.GetNotifications;

public class GetNotificationsValidator : AbstractValidator<GetNotificationsQuery>
{
    public GetNotificationsValidator()
    {
        RuleFor(x => x.Recipient)
            .MaximumLength(320);

        RuleFor(x => x.NotificationType)
            .MaximumLength(100);
    }
}
