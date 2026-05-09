using Notification.Domain.Enums;

namespace Notification.Application.DTOs;

public sealed record NotificationDeliveryAttemptDto(
    Guid Id,
    int AttemptNumber,
    string Provider,
    NotificationDeliveryAttemptStatus Status,
    DateTime StartedAtUtc,
    DateTime? CompletedAtUtc,
    string? ProviderMessageId,
    string? ErrorMessage);
