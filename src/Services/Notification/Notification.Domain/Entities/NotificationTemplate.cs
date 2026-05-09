using Notification.Domain.Common;
using Notification.Domain.Enums;
using Notification.Domain.Exceptions;

namespace Notification.Domain.Entities;

public class NotificationTemplate : AuditableEntity<Guid>
{
    private NotificationTemplate()
    {
    }

    public NotificationTemplate(
        string key,
        NotificationChannel channel,
        string subjectTemplate,
        string bodyTemplate,
        int version = 1) : base(Guid.NewGuid())
    {
        SetKey(key);
        SetContent(subjectTemplate, bodyTemplate);

        if (version <= 0)
        {
            throw new DomainException("Template version must be greater than zero.");
        }

        Channel = channel;
        Version = version;
        IsActive = true;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public string Key { get; private set; } = null!;
    public NotificationChannel Channel { get; private set; }
    public string SubjectTemplate { get; private set; } = null!;
    public string BodyTemplate { get; private set; } = null!;
    public int Version { get; private set; }
    public bool IsActive { get; private set; }

    public void UpdateContent(string subjectTemplate, string bodyTemplate)
    {
        SetContent(subjectTemplate, bodyTemplate);
        Version++;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    private void SetKey(string key)
    {
        Key = NormalizeRequired(key, "Template key cannot be empty.");
    }

    private void SetContent(string subjectTemplate, string bodyTemplate)
    {
        SubjectTemplate = NormalizeRequired(subjectTemplate, "Subject template cannot be empty.");
        BodyTemplate = NormalizeRequired(bodyTemplate, "Body template cannot be empty.");
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
