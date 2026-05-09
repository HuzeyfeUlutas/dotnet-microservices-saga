using Notification.Domain.Common;
using Notification.Domain.Enums;
using Notification.Domain.Exceptions;

namespace Notification.Domain.Entities;

public class RecipientPreference : AuditableEntity<Guid>
{
    private RecipientPreference()
    {
    }

    public RecipientPreference(
        string recipientId,
        NotificationChannel channel,
        string notificationType,
        bool isEnabled = true) : base(Guid.NewGuid())
    {
        SetRecipientId(recipientId);
        SetNotificationType(notificationType);

        Channel = channel;
        IsEnabled = isEnabled;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public string RecipientId { get; private set; } = null!;
    public NotificationChannel Channel { get; private set; }
    public string NotificationType { get; private set; } = null!;
    public bool IsEnabled { get; private set; }
    public string? DisabledReason { get; private set; }

    public void Enable()
    {
        IsEnabled = true;
        DisabledReason = null;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Disable(string reason)
    {
        IsEnabled = false;
        DisabledReason = NormalizeRequired(reason, "Disabled reason cannot be empty.");
        UpdatedAtUtc = DateTime.UtcNow;
    }

    private void SetRecipientId(string recipientId)
    {
        RecipientId = NormalizeRequired(recipientId, "Recipient id cannot be empty.");
    }

    private void SetNotificationType(string notificationType)
    {
        NotificationType = NormalizeRequired(notificationType, "Notification type cannot be empty.");
    }

    private static string NormalizeRequired(string value, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException(message);
        }

        return value.Trim();
    }
}
