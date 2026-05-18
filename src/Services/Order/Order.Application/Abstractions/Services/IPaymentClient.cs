namespace Order.Application.Abstractions.Services;

public interface IPaymentClient
{
    Task<PaymentInitiationResultDto> CreatePaymentAsync(
        Guid orderId,
        decimal amount,
        string currency,
        string idempotencyKey,
        string provider,
        string method,
        CancellationToken cancellationToken = default);
}
