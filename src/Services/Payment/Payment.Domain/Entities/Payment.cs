using Payment.Domain.Common;
using Payment.Domain.Enums;
using Payment.Domain.Exceptions;
using Payment.Domain.ValueObjects;

namespace Payment.Domain.Entities;

public class Payment : AuditableEntity<Guid>
{
    private readonly List<PaymentAttempt> _attempts = [];

    private Payment()
    {
    }

    public Payment(
        Guid orderId,
        Money amount,
        PaymentProviderType provider,
        PaymentMethodType method,
        string idempotencyKey) : base(Guid.NewGuid())
    {
        if (orderId == Guid.Empty)
        {
            throw new DomainException("Order id cannot be empty.");
        }

        OrderId = orderId;
        Amount = amount ?? throw new DomainException("Payment amount cannot be empty.");
        Provider = provider;
        Method = method;
        IdempotencyKey = NormalizeRequired(idempotencyKey, "Idempotency key cannot be empty.");
        Status = PaymentStatus.Pending;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid OrderId { get; private set; }
    public Money Amount { get; private set; } = null!;
    public PaymentProviderType Provider { get; private set; }
    public PaymentMethodType Method { get; private set; }
    public PaymentStatus Status { get; private set; }
    public string IdempotencyKey { get; private set; } = null!;
    public string? ProviderPaymentId { get; private set; }
    public string? ProviderTransactionId { get; private set; }
    public DateTime? AuthorizedAtUtc { get; private set; }
    public DateTime? AuthorizationFailedAtUtc { get; private set; }
    public DateTime? CapturedAtUtc { get; private set; }
    public DateTime? CaptureFailedAtUtc { get; private set; }
    public DateTime? RefundedAtUtc { get; private set; }
    public DateTime? RefundFailedAtUtc { get; private set; }
    public DateTime? CancelledAtUtc { get; private set; }
    public DateTime? AuthorizationVoidedAtUtc { get; private set; }
    public DateTime? AuthorizationVoidFailedAtUtc { get; private set; }
    public string? FailureReason { get; private set; }
    public uint RowVersion { get; private set; }
    public IReadOnlyCollection<PaymentAttempt> Attempts => _attempts.AsReadOnly();

    public PaymentAttempt StartAuthorization(string? idempotencyKey = null)
    {
        if (Status != PaymentStatus.Pending)
        {
            throw new DomainException("Authorization can only be started for a pending payment.");
        }

        return AddAttempt(PaymentAttemptType.Authorization, idempotencyKey);
    }

    public void RequireAction(string? providerPaymentId = null, string? providerActionReference = null)
    {
        var attempt = GetCurrentAttempt(PaymentAttemptType.Authorization);
        attempt.RequireAction(providerPaymentId, providerActionReference);

        Status = PaymentStatus.RequiresAction;
        ProviderPaymentId = NormalizeOptional(providerPaymentId) ?? ProviderPaymentId;
    }

    public void MarkAsAuthorized(string? providerPaymentId = null, string? providerTransactionId = null)
    {
        if (Status is not PaymentStatus.Pending and not PaymentStatus.RequiresAction)
        {
            throw new DomainException("Payment cannot be authorized from its current state.");
        }

        var attempt = GetCurrentAttempt(PaymentAttemptType.Authorization);
        attempt.MarkAsSucceeded(providerPaymentId, providerTransactionId);

        Status = PaymentStatus.Authorized;
        ProviderPaymentId = NormalizeOptional(providerPaymentId) ?? ProviderPaymentId;
        ProviderTransactionId = NormalizeOptional(providerTransactionId);
        AuthorizedAtUtc = DateTime.UtcNow;
        AuthorizationFailedAtUtc = null;
        FailureReason = null;
    }

    public void MarkAuthorizationAsFailed(string reason)
    {
        if (Status is not PaymentStatus.Pending and not PaymentStatus.RequiresAction)
        {
            throw new DomainException("Payment authorization cannot fail from its current state.");
        }

        var attempt = GetCurrentAttempt(PaymentAttemptType.Authorization);
        attempt.MarkAsFailed(reason);

        Status = PaymentStatus.AuthorizationFailed;
        AuthorizationFailedAtUtc = DateTime.UtcNow;
        FailureReason = NormalizeRequired(reason, "Failure reason cannot be empty.");
    }

    public PaymentAttempt StartCapture(string? idempotencyKey = null)
    {
        if (Status is not PaymentStatus.Authorized and not PaymentStatus.CaptureFailed)
        {
            throw new DomainException("Capture can only be started for an authorized payment.");
        }

        return AddAttempt(PaymentAttemptType.Capture, idempotencyKey);
    }

    public void MarkAsCaptured(string? providerTransactionId = null)
    {
        if (Status is not PaymentStatus.Authorized and not PaymentStatus.CaptureFailed)
        {
            throw new DomainException("Payment cannot be captured from its current state.");
        }

        var attempt = GetCurrentAttempt(PaymentAttemptType.Capture);
        attempt.MarkAsSucceeded(ProviderPaymentId, providerTransactionId);

        Status = PaymentStatus.Captured;
        ProviderTransactionId = NormalizeOptional(providerTransactionId) ?? ProviderTransactionId;
        CapturedAtUtc = DateTime.UtcNow;
        CaptureFailedAtUtc = null;
        FailureReason = null;
    }

    public void MarkCaptureAsFailed(string reason)
    {
        if (Status is not PaymentStatus.Authorized and not PaymentStatus.CaptureFailed)
        {
            throw new DomainException("Payment capture cannot fail from its current state.");
        }

        var attempt = GetCurrentAttempt(PaymentAttemptType.Capture);
        attempt.MarkAsFailed(reason);

        Status = PaymentStatus.CaptureFailed;
        CaptureFailedAtUtc = DateTime.UtcNow;
        FailureReason = NormalizeRequired(reason, "Failure reason cannot be empty.");
    }

    public PaymentAttempt StartRefund(string? idempotencyKey = null)
    {
        if (Status is not PaymentStatus.Captured and not PaymentStatus.RefundFailed)
        {
            throw new DomainException("Refund can only be started for a captured payment.");
        }

        return AddAttempt(PaymentAttemptType.Refund, idempotencyKey);
    }

    public void MarkAsRefunded(string? providerTransactionId = null)
    {
        if (Status is not PaymentStatus.Captured and not PaymentStatus.RefundFailed)
        {
            throw new DomainException("Payment cannot be refunded from its current state.");
        }

        var attempt = GetCurrentAttempt(PaymentAttemptType.Refund);
        attempt.MarkAsSucceeded(ProviderPaymentId, providerTransactionId);

        Status = PaymentStatus.Refunded;
        RefundedAtUtc = DateTime.UtcNow;
        RefundFailedAtUtc = null;
        FailureReason = null;
    }

    public void MarkRefundAsFailed(string reason)
    {
        if (Status is not PaymentStatus.Captured and not PaymentStatus.RefundFailed)
        {
            throw new DomainException("Payment refund cannot fail from its current state.");
        }

        var attempt = GetCurrentAttempt(PaymentAttemptType.Refund);
        attempt.MarkAsFailed(reason);

        Status = PaymentStatus.RefundFailed;
        RefundFailedAtUtc = DateTime.UtcNow;
        FailureReason = NormalizeRequired(reason, "Failure reason cannot be empty.");
    }

    public PaymentAttempt StartAuthorizationVoid(string? idempotencyKey = null)
    {
        if (Status is not PaymentStatus.Authorized and
            not PaymentStatus.CaptureFailed and
            not PaymentStatus.AuthorizationVoidFailed)
        {
            throw new DomainException("Authorization void can only be started for an authorized payment.");
        }

        return AddAttempt(PaymentAttemptType.AuthorizationVoid, idempotencyKey);
    }

    public void MarkAuthorizationAsVoided(string? providerTransactionId = null)
    {
        if (Status is not PaymentStatus.Authorized and
            not PaymentStatus.CaptureFailed and
            not PaymentStatus.AuthorizationVoidFailed)
        {
            throw new DomainException("Payment authorization cannot be voided from its current state.");
        }

        var attempt = GetCurrentAttempt(PaymentAttemptType.AuthorizationVoid);
        attempt.MarkAsSucceeded(ProviderPaymentId, providerTransactionId);

        Status = PaymentStatus.AuthorizationVoided;
        ProviderTransactionId = NormalizeOptional(providerTransactionId) ?? ProviderTransactionId;
        AuthorizationVoidedAtUtc = DateTime.UtcNow;
        AuthorizationVoidFailedAtUtc = null;
        FailureReason = null;
    }

    public void MarkAuthorizationVoidAsFailed(string reason)
    {
        if (Status is not PaymentStatus.Authorized and
            not PaymentStatus.CaptureFailed and
            not PaymentStatus.AuthorizationVoidFailed)
        {
            throw new DomainException("Payment authorization void cannot fail from its current state.");
        }

        var attempt = GetCurrentAttempt(PaymentAttemptType.AuthorizationVoid);
        attempt.MarkAsFailed(reason);

        Status = PaymentStatus.AuthorizationVoidFailed;
        AuthorizationVoidFailedAtUtc = DateTime.UtcNow;
        FailureReason = NormalizeRequired(reason, "Failure reason cannot be empty.");
    }

    public void CancelPending(string reason)
    {
        if (Status == PaymentStatus.Cancelled)
        {
            return;
        }

        if (Status is not PaymentStatus.Pending and not PaymentStatus.RequiresAction)
        {
            throw new DomainException("Only pending payment can be cancelled.");
        }

        var activeAttempt = _attempts.SingleOrDefault(x =>
            x.Status is PaymentAttemptStatus.Processing or PaymentAttemptStatus.RequiresAction);
        activeAttempt?.Cancel(reason);

        Status = PaymentStatus.Cancelled;
        CancelledAtUtc = DateTime.UtcNow;
        FailureReason = NormalizeRequired(reason, "Cancellation reason cannot be empty.");
    }

    private PaymentAttempt AddAttempt(PaymentAttemptType type, string? idempotencyKey)
    {
        if (_attempts.Any(x => x.Status is PaymentAttemptStatus.Processing or PaymentAttemptStatus.RequiresAction))
        {
            throw new DomainException("Payment already has an active attempt.");
        }

        var attempt = new PaymentAttempt(Id, _attempts.Count + 1, type, Provider, idempotencyKey);
        _attempts.Add(attempt);

        return attempt;
    }

    private PaymentAttempt GetCurrentAttempt(PaymentAttemptType type)
    {
        var attempt = _attempts
            .Where(x => x.Type == type)
            .OrderByDescending(x => x.AttemptNumber)
            .FirstOrDefault();

        if (attempt is null)
        {
            throw new DomainException("Payment attempt was not found.");
        }

        return attempt;
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
