namespace Marketplace.Contracts.Payment.V1;

public sealed record CapturePaymentRequested(
    Guid EventId,
    Guid PaymentId,
    Guid OrderId,
    DateTime OccurredAtUtc);

public sealed record RefundPaymentRequested(
    Guid EventId,
    Guid PaymentId,
    Guid OrderId,
    string? Reason,
    DateTime OccurredAtUtc);

public sealed record VoidPaymentAuthorizationRequested(
    Guid EventId,
    Guid PaymentId,
    Guid OrderId,
    string? Reason,
    DateTime OccurredAtUtc);

public sealed record CancelPendingPaymentRequested(
    Guid EventId,
    Guid PaymentId,
    Guid OrderId,
    string Reason,
    DateTime OccurredAtUtc);

public sealed record PaymentAuthorized(
    Guid EventId,
    Guid PaymentId,
    Guid OrderId,
    decimal Amount,
    string Currency,
    DateTime OccurredAtUtc);

public sealed record PaymentAuthorizationFailed(
    Guid EventId,
    Guid PaymentId,
    Guid OrderId,
    decimal Amount,
    string Currency,
    string FailureReason,
    DateTime OccurredAtUtc);

public sealed record PaymentCaptured(
    Guid EventId,
    Guid PaymentId,
    Guid OrderId,
    decimal Amount,
    string Currency,
    DateTime OccurredAtUtc);

public sealed record PaymentCaptureFailed(
    Guid EventId,
    Guid PaymentId,
    Guid OrderId,
    decimal Amount,
    string Currency,
    string FailureReason,
    DateTime OccurredAtUtc);

public sealed record PaymentRefunded(
    Guid EventId,
    Guid PaymentId,
    Guid OrderId,
    decimal Amount,
    string Currency,
    DateTime OccurredAtUtc);

public sealed record PaymentRefundFailed(
    Guid EventId,
    Guid PaymentId,
    Guid OrderId,
    decimal Amount,
    string Currency,
    string FailureReason,
    DateTime OccurredAtUtc);

public sealed record PaymentAuthorizationVoided(
    Guid EventId,
    Guid RequestEventId,
    Guid PaymentId,
    Guid OrderId,
    DateTime OccurredAtUtc);

public sealed record PaymentAuthorizationVoidFailed(
    Guid EventId,
    Guid RequestEventId,
    Guid PaymentId,
    Guid OrderId,
    string FailureReason,
    DateTime OccurredAtUtc);

public sealed record PaymentCancelled(
    Guid EventId,
    Guid RequestEventId,
    Guid PaymentId,
    Guid OrderId,
    DateTime OccurredAtUtc);

public sealed record PaymentCancellationFailed(
    Guid EventId,
    Guid RequestEventId,
    Guid PaymentId,
    Guid OrderId,
    string FailureReason,
    DateTime OccurredAtUtc);
