using MediatR;

namespace Notification.Application.Features.Notifications.CreateNotification;

public sealed record CreateNotificationCommand(
    string NotificationType,
    string RecipientId,
    string Recipient,
    string Subject,
    string Body,
    Guid? SourceEventId = null,
    string? CorrelationId = null,
    DateTime? ScheduledAtUtc = null) : IRequest<Guid>;
