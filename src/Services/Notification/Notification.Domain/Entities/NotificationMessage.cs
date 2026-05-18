using Notification.Domain.Common;
using Notification.Domain.Enums;
using Notification.Domain.Exceptions;

namespace Notification.Domain.Entities;

public class NotificationMessage : AuditableEntity<Guid>
{
    private readonly List<NotificationDeliveryAttempt> _deliveryAttempts = [];

    private NotificationMessage()
    {
    }

    public NotificationMessage(
        NotificationChannel channel,
        string notificationType,
        string recipientId,
        string recipient,
        string subject,
        string body,
        Guid? sourceEventId = null,
        string? correlationId = null,
        DateTime? scheduledAtUtc = null) : base(Guid.NewGuid())
    {
        SetNotificationType(notificationType);
        SetRecipientId(recipientId);
        SetRecipient(recipient);
        SetContent(subject, body);

        Channel = channel;
        SourceEventId = sourceEventId;
        CorrelationId = NormalizeOptional(correlationId);
        ScheduledAtUtc = scheduledAtUtc;
        Status = NotificationMessageStatus.Pending;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public NotificationChannel Channel { get; private set; }
    public string NotificationType { get; private set; } = null!;
    public string RecipientId { get; private set; } = null!;
    public string Recipient { get; private set; } = null!;
    public string Subject { get; private set; } = null!;
    public string Body { get; private set; } = null!;
    public NotificationMessageStatus Status { get; private set; }
    public Guid? SourceEventId { get; private set; }
    public string? CorrelationId { get; private set; }
    public DateTime? ScheduledAtUtc { get; private set; }
    public DateTime? ProcessingStartedAtUtc { get; private set; }
    public DateTime? SentAtUtc { get; private set; }
    public DateTime? FailedAtUtc { get; private set; }
    public string? FailureReason { get; private set; }
    public DateTime? CancelledAtUtc { get; private set; }
    public string? CancellationReason { get; private set; }
    public DateTime? SkippedAtUtc { get; private set; }
    public string? SkipReason { get; private set; }
    public long ConcurrencyVersion { get; private set; }
    public IReadOnlyCollection<NotificationDeliveryAttempt> DeliveryAttempts => _deliveryAttempts.AsReadOnly();

    public NotificationDeliveryAttempt StartDeliveryAttempt(string provider)
    {
        EnsureCanStartDeliveryAttempt();

        Status = NotificationMessageStatus.Processing;
        ProcessingStartedAtUtc = DateTime.UtcNow;
        FailureReason = null;
        FailedAtUtc = null;
        UpdatedAtUtc = DateTime.UtcNow;
        ConcurrencyVersion++;

        var attempt = new NotificationDeliveryAttempt(Id, _deliveryAttempts.Count + 1, provider);
        _deliveryAttempts.Add(attempt);

        return attempt;
    }

    public void MarkAsSent(string? providerMessageId = null)
    {
        if (Status is NotificationMessageStatus.Cancelled or NotificationMessageStatus.Skipped)
        {
            throw new DomainException("Cancelled or skipped notification cannot be marked as sent.");
        }

        Status = NotificationMessageStatus.Sent;
        SentAtUtc = DateTime.UtcNow;
        FailedAtUtc = null;
        FailureReason = null;
        UpdatedAtUtc = DateTime.UtcNow;
        ConcurrencyVersion++;

        var currentAttempt = GetCurrentAttemptOrDefault();
        if (currentAttempt is not null && currentAttempt.Status == NotificationDeliveryAttemptStatus.Processing)
        {
            currentAttempt.MarkAsSucceeded(providerMessageId);
        }
    }

    public void MarkAsFailed(string reason)
    {
        if (Status is NotificationMessageStatus.Sent or NotificationMessageStatus.Cancelled or NotificationMessageStatus.Skipped)
        {
            throw new DomainException("Sent, cancelled, or skipped notification cannot be marked as failed.");
        }

        Status = NotificationMessageStatus.Failed;
        FailedAtUtc = DateTime.UtcNow;
        FailureReason = NormalizeRequired(reason, "Failure reason cannot be empty.");
        UpdatedAtUtc = DateTime.UtcNow;
        ConcurrencyVersion++;

        var currentAttempt = GetCurrentAttemptOrDefault();
        if (currentAttempt is not null && currentAttempt.Status == NotificationDeliveryAttemptStatus.Processing)
        {
            currentAttempt.MarkAsFailed(FailureReason);
        }
    }

    public void Cancel(string reason)
    {
        if (Status == NotificationMessageStatus.Sent)
        {
            throw new DomainException("Sent notification cannot be cancelled.");
        }

        Status = NotificationMessageStatus.Cancelled;
        CancelledAtUtc = DateTime.UtcNow;
        CancellationReason = NormalizeRequired(reason, "Cancellation reason cannot be empty.");
        UpdatedAtUtc = DateTime.UtcNow;
        ConcurrencyVersion++;
    }

    public void Skip(string reason)
    {
        if (Status == NotificationMessageStatus.Sent)
        {
            throw new DomainException("Sent notification cannot be skipped.");
        }

        Status = NotificationMessageStatus.Skipped;
        SkippedAtUtc = DateTime.UtcNow;
        SkipReason = NormalizeRequired(reason, "Skip reason cannot be empty.");
        UpdatedAtUtc = DateTime.UtcNow;
        ConcurrencyVersion++;
    }

    private void EnsureCanStartDeliveryAttempt()
    {
        if (Status == NotificationMessageStatus.Processing)
        {
            throw new DomainException("Notification is already being processed.");
        }

        if (Status is NotificationMessageStatus.Sent or NotificationMessageStatus.Cancelled or NotificationMessageStatus.Skipped)
        {
            throw new DomainException("Delivery attempt cannot be started for a completed notification.");
        }
    }

    private NotificationDeliveryAttempt? GetCurrentAttemptOrDefault()
    {
        return _deliveryAttempts
            .OrderByDescending(x => x.AttemptNumber)
            .FirstOrDefault();
    }

    private void SetNotificationType(string notificationType)
    {
        NotificationType = NormalizeRequired(notificationType, "Notification type cannot be empty.");
    }

    private void SetRecipient(string recipient)
    {
        Recipient = NormalizeRequired(recipient, "Recipient cannot be empty.");
    }

    private void SetRecipientId(string recipientId)
    {
        RecipientId = NormalizeRequired(recipientId, "Recipient id cannot be empty.");
    }

    private void SetContent(string subject, string body)
    {
        Subject = NormalizeRequired(subject, "Subject cannot be empty.");
        Body = NormalizeRequired(body, "Body cannot be empty.");
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
