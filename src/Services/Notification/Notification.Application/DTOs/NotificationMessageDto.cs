using Notification.Domain.Enums;

namespace Notification.Application.DTOs;

public sealed record NotificationMessageDto(
    Guid Id,
    NotificationChannel Channel,
    string NotificationType,
    string Recipient,
    string Subject,
    string Body,
    NotificationMessageStatus Status,
    Guid? SourceEventId,
    string? CorrelationId,
    DateTime? ScheduledAtUtc,
    DateTime? ProcessingStartedAtUtc,
    DateTime? SentAtUtc,
    DateTime? FailedAtUtc,
    string? FailureReason,
    DateTime CreatedAtUtc,
    IReadOnlyCollection<NotificationDeliveryAttemptDto> DeliveryAttempts);
