using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using Marketplace.Contracts.Payment.V1;
using Payment.Application.Abstractions.Messaging;
using Payment.Application.Features.Payments.CapturePayment;
using Payment.Domain.Enums;

namespace Payment.Infrastructure.Messaging.Consumers;

public sealed class CapturePaymentRequestedConsumer(
    ISender sender,
    IIntegrationEventPublisher integrationEventPublisher,
    ILogger<CapturePaymentRequestedConsumer> logger) : IConsumer<CapturePaymentRequested>
{
    public async Task Consume(ConsumeContext<CapturePaymentRequested> context)
    {
        var message = context.Message;

        logger.LogInformation(
            "Consuming capture payment request event {EventId} for payment {PaymentId} and order {OrderId}",
            message.EventId,
            message.PaymentId,
            message.OrderId);

        var payment = await sender.Send(
            new CapturePaymentCommand(message.PaymentId, message.IdempotencyKey),
            context.CancellationToken);

        if (payment.Status == PaymentStatus.Captured)
        {
            await integrationEventPublisher.PublishAsync(
                new PaymentCaptured(
                    Guid.NewGuid(),
                    payment.Id,
                    payment.OrderId,
                    payment.Amount,
                    payment.Currency,
                    DateTime.UtcNow),
                context.CancellationToken);
            return;
        }

        await integrationEventPublisher.PublishAsync(
            new PaymentCaptureFailed(
                Guid.NewGuid(),
                payment.Id,
                payment.OrderId,
                payment.Amount,
                payment.Currency,
                payment.FailureReason ?? "Payment capture failed.",
                DateTime.UtcNow),
            context.CancellationToken);
    }
}
