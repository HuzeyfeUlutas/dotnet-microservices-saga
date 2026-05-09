using Payment.Domain.Enums;

namespace Payment.Application.DTOs;

public sealed record PaymentDto(
    Guid Id,
    Guid OrderId,
    decimal Amount,
    string Currency,
    PaymentProviderType Provider,
    PaymentMethodType Method,
    PaymentStatus Status,
    string IdempotencyKey,
    DateTime CreatedAtUtc,
    DateTime? AuthorizedAtUtc,
    DateTime? CapturedAtUtc,
    DateTime? RefundedAtUtc,
    string? FailureReason);
