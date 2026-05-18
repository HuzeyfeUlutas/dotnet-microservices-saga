namespace Notification.Application.Contracts.IntegrationEvents.Notifications;

public sealed record TemplateNotificationRequestedIntegrationEvent(
    Guid EventId,
    string NotificationType,
    string TemplateKey,
    string RecipientId,
    string Recipient,
    IReadOnlyDictionary<string, string> Variables,
    DateTime? ScheduledAtUtc,
    DateTime OccurredAtUtc);
