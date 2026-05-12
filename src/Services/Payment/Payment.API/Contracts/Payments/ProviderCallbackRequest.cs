namespace Payment.API.Contracts.Payments;

public sealed record ProviderCallbackRequest(
    Guid PaymentId,
    bool Approved,
    string? ProviderEventId = null);
