namespace Payment.Application.DTOs;

public sealed record ProviderPaymentResultDto(
    bool Succeeded,
    string? ProviderPaymentId = null,
    string? ProviderTransactionId = null,
    string? FailureReason = null);
