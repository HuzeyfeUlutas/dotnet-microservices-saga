using MediatR;
using Microsoft.EntityFrameworkCore;
using Notification.Application.Abstractions.Persistence;
using Notification.Application.Common.Exceptions;
using Notification.Domain.Entities;

namespace Notification.Application.Features.NotificationPreferences.SetRecipientPreference;

public class SetRecipientPreferenceHandler(INotificationDbContext context)
    : IRequestHandler<SetRecipientPreferenceCommand>
{
    public async Task Handle(SetRecipientPreferenceCommand request, CancellationToken cancellationToken)
    {
        var recipientId = request.RecipientId.Trim();
        var notificationType = request.NotificationType.Trim();

        var preference = await context.RecipientPreferences.FirstOrDefaultAsync(
            x => x.RecipientId == recipientId &&
                 x.Channel == request.Channel &&
                 x.NotificationType == notificationType,
            cancellationToken);

        if (preference is null)
        {
            preference = new RecipientPreference(
                recipientId,
                request.Channel,
                notificationType,
                request.IsEnabled);

            context.RecipientPreferences.Add(preference);
        }

        if (request.IsEnabled)
        {
            preference.Enable();
        }
        else
        {
            preference.Disable(request.DisabledReason!);
        }

        await SaveChangesAsync(cancellationToken);
    }

    private async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            throw new ConflictException($"Recipient preference could not be saved. {exception.Message}");
        }
    }
}
