using Payment.Application.DTOs;
using PaymentEntity = Payment.Domain.Entities.Payment;

namespace Payment.Application.Abstractions.Providers;

public interface IPaymentProvider
{
    Task<PaymentActionDto> StartAuthorizationAsync(PaymentEntity payment, CancellationToken cancellationToken = default);

    Task<ProviderPaymentResultDto> CompleteAuthorizationAsync(PaymentEntity payment, bool approved, CancellationToken cancellationToken = default);

    Task<ProviderPaymentResultDto> CaptureAsync(PaymentEntity payment, CancellationToken cancellationToken = default);

    Task<ProviderPaymentResultDto> RefundAsync(PaymentEntity payment, CancellationToken cancellationToken = default);

    Task<ProviderPaymentResultDto> VoidAuthorizationAsync(PaymentEntity payment, CancellationToken cancellationToken = default);
}
