using Notification.Domain.Enums;

namespace Notification.Application.DTOs;

public sealed record RecipientPreferenceDto(
    Guid Id,
    string RecipientId,
    NotificationChannel Channel,
    string NotificationType,
    bool IsEnabled,
    string? DisabledReason);
