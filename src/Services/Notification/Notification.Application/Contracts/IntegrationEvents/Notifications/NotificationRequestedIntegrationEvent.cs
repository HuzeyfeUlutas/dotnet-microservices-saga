namespace Notification.Application.Contracts.IntegrationEvents.Notifications;

public sealed record NotificationRequestedIntegrationEvent(
    Guid EventId,
    string NotificationType,
    string RecipientId,
    string Recipient,
    string Subject,
    string Body,
    DateTime? ScheduledAtUtc,
    DateTime OccurredAtUtc);
