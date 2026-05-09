using FluentValidation;

namespace Notification.Application.Features.NotificationPreferences.GetRecipientPreferences;

public class GetRecipientPreferencesValidator : AbstractValidator<GetRecipientPreferencesQuery>
{
    public GetRecipientPreferencesValidator()
    {
        RuleFor(x => x.RecipientId)
            .NotEmpty()
            .MaximumLength(100);
    }
}
