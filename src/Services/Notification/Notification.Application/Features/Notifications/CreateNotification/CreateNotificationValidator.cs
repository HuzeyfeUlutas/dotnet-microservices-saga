using FluentValidation;

namespace Notification.Application.Features.Notifications.CreateNotification;

public class CreateNotificationValidator : AbstractValidator<CreateNotificationCommand>
{
    public CreateNotificationValidator()
    {
        RuleFor(x => x.NotificationType)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.RecipientId)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Recipient)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(320);

        RuleFor(x => x.Subject)
            .NotEmpty()
            .MaximumLength(250);

        RuleFor(x => x.Body)
            .NotEmpty();

        RuleFor(x => x.CorrelationId)
            .MaximumLength(100);

        RuleFor(x => x.ScheduledAtUtc)
            .Must(scheduledAtUtc => !scheduledAtUtc.HasValue || scheduledAtUtc.Value > DateTime.UtcNow)
            .WithMessage("Scheduled date must be in the future.")
            .When(x => x.ScheduledAtUtc.HasValue);
    }
}
