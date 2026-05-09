using Notification.Domain.Enums;

namespace Notification.API.Contracts.NotificationPreferences;

public sealed record SetRecipientPreferenceRequest(
    NotificationChannel Channel,
    string NotificationType,
    bool IsEnabled,
    string? DisabledReason);
