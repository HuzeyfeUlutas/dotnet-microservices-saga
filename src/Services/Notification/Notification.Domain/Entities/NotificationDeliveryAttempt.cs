using Notification.Domain.Common;
using Notification.Domain.Enums;
using Notification.Domain.Exceptions;

namespace Notification.Domain.Entities;

public class NotificationDeliveryAttempt : BaseEntity<Guid>
{
    private NotificationDeliveryAttempt()
    {
    }

    internal NotificationDeliveryAttempt(Guid notificationMessageId, int attemptNumber, string provider) : base(Guid.NewGuid())
    {
        if (notificationMessageId == Guid.Empty)
        {
            throw new DomainException("Notification message id cannot be empty.");
        }

        if (attemptNumber <= 0)
        {
            throw new DomainException("Attempt number must be greater than zero.");
        }

        NotificationMessageId = notificationMessageId;
        AttemptNumber = attemptNumber;
        Provider = NormalizeRequired(provider, "Provider cannot be empty.");
        Status = NotificationDeliveryAttemptStatus.Processing;
        StartedAtUtc = DateTime.UtcNow;
    }

    public Guid NotificationMessageId { get; private set; }
    public int AttemptNumber { get; private set; }
    public string Provider { get; private set; } = null!;
    public NotificationDeliveryAttemptStatus Status { get; private set; }
    public DateTime StartedAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }
    public string? ProviderMessageId { get; private set; }
    public string? ErrorMessage { get; private set; }

    internal void MarkAsSucceeded(string? providerMessageId = null)
    {
        EnsureProcessing("succeeded");

        Status = NotificationDeliveryAttemptStatus.Succeeded;
        CompletedAtUtc = DateTime.UtcNow;
        ProviderMessageId = NormalizeOptional(providerMessageId);
        ErrorMessage = null;
    }

    internal void MarkAsFailed(string errorMessage)
    {
        EnsureProcessing("failed");

        Status = NotificationDeliveryAttemptStatus.Failed;
        CompletedAtUtc = DateTime.UtcNow;
        ErrorMessage = NormalizeRequired(errorMessage, "Error message cannot be empty.");
    }

    private void EnsureProcessing(string action)
    {
        if (Status != NotificationDeliveryAttemptStatus.Processing)
        {
            throw new DomainException($"Delivery attempt cannot be marked as {action}.");
        }
    }

    private static string NormalizeRequired(string value, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException(message);
        }

        return value.Trim();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
