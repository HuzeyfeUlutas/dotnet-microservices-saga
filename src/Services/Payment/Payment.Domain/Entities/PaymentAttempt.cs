using Payment.Domain.Common;
using Payment.Domain.Enums;
using Payment.Domain.Exceptions;

namespace Payment.Domain.Entities;

public class PaymentAttempt : BaseEntity<Guid>
{
    private PaymentAttempt()
    {
    }

    internal PaymentAttempt(
        Guid paymentId,
        int attemptNumber,
        PaymentAttemptType type,
        PaymentProviderType provider,
        string? idempotencyKey = null) : base(Guid.NewGuid())
    {
        if (paymentId == Guid.Empty)
        {
            throw new DomainException("Payment id cannot be empty.");
        }

        if (attemptNumber <= 0)
        {
            throw new DomainException("Attempt number must be greater than zero.");
        }

        PaymentId = paymentId;
        AttemptNumber = attemptNumber;
        Type = type;
        Provider = provider;
        IdempotencyKey = NormalizeOptional(idempotencyKey);
        Status = PaymentAttemptStatus.Processing;
        StartedAtUtc = DateTime.UtcNow;
    }

    public Guid PaymentId { get; private set; }
    public int AttemptNumber { get; private set; }
    public PaymentAttemptType Type { get; private set; }
    public PaymentProviderType Provider { get; private set; }
    public PaymentAttemptStatus Status { get; private set; }
    public string? IdempotencyKey { get; private set; }
    public string? ProviderPaymentId { get; private set; }
    public string? ProviderTransactionId { get; private set; }
    public string? ProviderActionReference { get; private set; }
    public DateTime StartedAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }
    public string? FailureReason { get; private set; }

    internal void RequireAction(string? providerPaymentId = null, string? providerActionReference = null)
    {
        EnsureProcessing("require action");

        Status = PaymentAttemptStatus.RequiresAction;
        ProviderPaymentId = NormalizeOptional(providerPaymentId);
        ProviderActionReference = NormalizeOptional(providerActionReference);
    }

    internal void MarkAsSucceeded(string? providerPaymentId = null, string? providerTransactionId = null)
    {
        EnsureInProgress("succeeded");

        Status = PaymentAttemptStatus.Succeeded;
        ProviderPaymentId = NormalizeOptional(providerPaymentId) ?? ProviderPaymentId;
        ProviderTransactionId = NormalizeOptional(providerTransactionId);
        CompletedAtUtc = DateTime.UtcNow;
        FailureReason = null;
    }

    internal void MarkAsFailed(string reason)
    {
        EnsureInProgress("failed");

        Status = PaymentAttemptStatus.Failed;
        CompletedAtUtc = DateTime.UtcNow;
        FailureReason = NormalizeRequired(reason, "Failure reason cannot be empty.");
    }

    private void EnsureProcessing(string action)
    {
        if (Status != PaymentAttemptStatus.Processing)
        {
            throw new DomainException($"Payment attempt cannot {action}.");
        }
    }

    private void EnsureInProgress(string action)
    {
        if (Status is not PaymentAttemptStatus.Processing and not PaymentAttemptStatus.RequiresAction)
        {
            throw new DomainException($"Payment attempt cannot be marked as {action}.");
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
