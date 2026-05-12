using Payment.Domain.Enums;

namespace Payment.API.Contracts.Payments;

public sealed record CreatePaymentRequest(
    Guid OrderId,
    decimal Amount,
    string Currency,
    string IdempotencyKey,
    PaymentProviderType Provider = PaymentProviderType.Fake,
    PaymentMethodType Method = PaymentMethodType.Card);
