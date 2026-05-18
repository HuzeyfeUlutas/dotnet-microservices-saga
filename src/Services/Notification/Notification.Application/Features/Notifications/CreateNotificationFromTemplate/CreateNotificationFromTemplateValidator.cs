using FluentValidation;

namespace Notification.Application.Features.Notifications.CreateNotificationFromTemplate;

public class CreateNotificationFromTemplateValidator : AbstractValidator<CreateNotificationFromTemplateCommand>
{
    public CreateNotificationFromTemplateValidator()
    {
        RuleFor(x => x.NotificationType)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.TemplateKey)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.RecipientId)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Recipient)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(320);

        RuleFor(x => x.Variables)
            .NotNull();
    }
}
