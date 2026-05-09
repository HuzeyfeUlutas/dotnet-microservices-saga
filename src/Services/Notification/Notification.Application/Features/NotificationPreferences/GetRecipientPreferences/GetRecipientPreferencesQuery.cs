using MediatR;
using Notification.Application.DTOs;

namespace Notification.Application.Features.NotificationPreferences.GetRecipientPreferences;

public sealed record GetRecipientPreferencesQuery(string RecipientId)
    : IRequest<IReadOnlyCollection<RecipientPreferenceDto>>;
