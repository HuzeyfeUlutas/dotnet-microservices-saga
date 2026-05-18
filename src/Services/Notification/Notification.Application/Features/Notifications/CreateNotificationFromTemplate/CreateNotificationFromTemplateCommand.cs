using MediatR;

namespace Notification.Application.Features.Notifications.CreateNotificationFromTemplate;

public sealed record CreateNotificationFromTemplateCommand(
    string NotificationType,
    string TemplateKey,
    string RecipientId,
    string Recipient,
    IReadOnlyDictionary<string, string> Variables,
    Guid? SourceEventId = null,
    string? CorrelationId = null,
    DateTime? ScheduledAtUtc = null) : IRequest<Guid>;
