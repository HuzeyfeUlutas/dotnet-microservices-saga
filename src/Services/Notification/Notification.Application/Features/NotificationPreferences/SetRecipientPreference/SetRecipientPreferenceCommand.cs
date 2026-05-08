using MediatR;
using Notification.Domain.Enums;

namespace Notification.Application.Features.NotificationPreferences.SetRecipientPreference;

public sealed record SetRecipientPreferenceCommand(
    string RecipientId,
    NotificationChannel Channel,
    string NotificationType,
    bool IsEnabled,
    string? DisabledReason) : IRequest;
