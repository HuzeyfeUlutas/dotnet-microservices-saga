using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using Marketplace.Contracts.Payment.V1;
using Payment.Application.Abstractions.Messaging;
using Payment.Application.Features.Payments.RefundPayment;
using Payment.Domain.Enums;

namespace Payment.Infrastructure.Messaging.Consumers;

public sealed class RefundPaymentRequestedConsumer(
    ISender sender,
    IIntegrationEventPublisher integrationEventPublisher,
    ILogger<RefundPaymentRequestedConsumer> logger) : IConsumer<RefundPaymentRequested>
{
    public async Task Consume(ConsumeContext<RefundPaymentRequested> context)
    {
        var message = context.Message;

        logger.LogInformation(
            "Consuming refund payment request event {EventId} for payment {PaymentId} and order {OrderId}",
            message.EventId,
            message.PaymentId,
            message.OrderId);

        var payment = await sender.Send(
            new RefundPaymentCommand(message.PaymentId, message.IdempotencyKey),
            context.CancellationToken);

        if (payment.Status == PaymentStatus.Refunded)
        {
            await integrationEventPublisher.PublishAsync(
                new PaymentRefunded(
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
            new PaymentRefundFailed(
                Guid.NewGuid(),
                payment.Id,
                payment.OrderId,
                payment.Amount,
                payment.Currency,
                payment.FailureReason ?? "Payment refund failed.",
                DateTime.UtcNow),
            context.CancellationToken);
    }
}
