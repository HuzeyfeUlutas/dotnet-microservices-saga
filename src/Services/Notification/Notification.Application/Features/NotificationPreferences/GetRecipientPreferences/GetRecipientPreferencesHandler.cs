using MediatR;
using Microsoft.EntityFrameworkCore;
using Notification.Application.Abstractions.Persistence;
using Notification.Application.DTOs;

namespace Notification.Application.Features.NotificationPreferences.GetRecipientPreferences;

public class GetRecipientPreferencesHandler(INotificationDbContext context)
    : IRequestHandler<GetRecipientPreferencesQuery, IReadOnlyCollection<RecipientPreferenceDto>>
{
    public async Task<IReadOnlyCollection<RecipientPreferenceDto>> Handle(
        GetRecipientPreferencesQuery request,
        CancellationToken cancellationToken)
    {
        var preferences = await context.RecipientPreferences
            .AsNoTracking()
            .Where(x => x.RecipientId == request.RecipientId.Trim())
            .OrderBy(x => x.NotificationType)
            .Select(x => new RecipientPreferenceDto(
                x.Id,
                x.RecipientId,
                x.Channel,
                x.NotificationType,
                x.IsEnabled,
                x.DisabledReason))
            .ToListAsync(cancellationToken);

        return preferences;
    }
}
