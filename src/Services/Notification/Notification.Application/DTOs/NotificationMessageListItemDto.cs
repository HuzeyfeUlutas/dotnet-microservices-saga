using Notification.Domain.Enums;

namespace Notification.Application.DTOs;

public sealed record NotificationMessageListItemDto(
    Guid Id,
    NotificationChannel Channel,
    string NotificationType,
    string Recipient,
    string Subject,
    NotificationMessageStatus Status,
    Guid? SourceEventId,
    string? CorrelationId,
    DateTime? ScheduledAtUtc,
    DateTime? SentAtUtc,
    DateTime? FailedAtUtc,
    string? FailureReason,
    DateTime CreatedAtUtc);
