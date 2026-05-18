namespace Order.Application.Abstractions.Services;

public sealed record PaymentInitiationResultDto(
    Guid PaymentId,
    string Status,
    string Provider,
    PaymentActionDto Action);
