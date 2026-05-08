using FluentValidation;

namespace Notification.Application.Features.NotificationPreferences.SetRecipientPreference;

public class SetRecipientPreferenceValidator : AbstractValidator<SetRecipientPreferenceCommand>
{
    public SetRecipientPreferenceValidator()
    {
        RuleFor(x => x.RecipientId)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.NotificationType)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.DisabledReason)
            .NotEmpty()
            .MaximumLength(500)
            .When(x => !x.IsEnabled);

        RuleFor(x => x.DisabledReason)
            .MaximumLength(500);

        RuleFor(x => x.Channel)
            .IsInEnum();
    }
}
