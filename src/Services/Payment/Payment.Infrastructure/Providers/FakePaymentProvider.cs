using Payment.Application.Abstractions.Providers;
using Payment.Application.DTOs;
using PaymentEntity = Payment.Domain.Entities.Payment;

namespace Payment.Infrastructure.Providers;

public class FakePaymentProvider : IPaymentProvider
{
    public Task<PaymentActionDto> StartAuthorizationAsync(PaymentEntity payment, CancellationToken cancellationToken = default)
    {
        var action = new PaymentActionDto(
            Type: "Redirect",
            RedirectUrl: $"/fake-3ds/payments/{payment.Id}");

        return Task.FromResult(action);
    }

    public Task<ProviderPaymentResultDto> CompleteAuthorizationAsync(
        PaymentEntity payment,
        bool approved,
        CancellationToken cancellationToken = default)
    {
        var result = approved
            ? new ProviderPaymentResultDto(
                Succeeded: true,
                ProviderPaymentId: CreateProviderPaymentId(payment.Id),
                ProviderTransactionId: CreateProviderTransactionId(payment.Id, "auth"))
            : new ProviderPaymentResultDto(
                Succeeded: false,
                ProviderPaymentId: CreateProviderPaymentId(payment.Id),
                FailureReason: "Fake 3DS payment was declined.");

        return Task.FromResult(result);
    }

    public Task<ProviderPaymentResultDto> CaptureAsync(PaymentEntity payment, CancellationToken cancellationToken = default)
    {
        var result = new ProviderPaymentResultDto(
            Succeeded: true,
            ProviderPaymentId: payment.ProviderPaymentId ?? CreateProviderPaymentId(payment.Id),
            ProviderTransactionId: CreateProviderTransactionId(payment.Id, "capture"));

        return Task.FromResult(result);
    }

    public Task<ProviderPaymentResultDto> RefundAsync(PaymentEntity payment, CancellationToken cancellationToken = default)
    {
        var result = new ProviderPaymentResultDto(
            Succeeded: true,
            ProviderPaymentId: payment.ProviderPaymentId ?? CreateProviderPaymentId(payment.Id),
            ProviderTransactionId: CreateProviderTransactionId(payment.Id, "refund"));

        return Task.FromResult(result);
    }

    private static string CreateProviderPaymentId(Guid paymentId)
    {
        return $"fake-pay-{paymentId:N}";
    }

    private static string CreateProviderTransactionId(Guid paymentId, string operation)
    {
        return $"fake-{operation}-{paymentId:N}";
    }
}
