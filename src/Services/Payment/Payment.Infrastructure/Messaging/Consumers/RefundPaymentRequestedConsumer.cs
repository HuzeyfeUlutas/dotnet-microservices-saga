using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using Marketplace.Contracts.Payment.V1;
using Payment.Application.Features.Payments.RefundPayment;

namespace Payment.Infrastructure.Messaging.Consumers;

public sealed class RefundPaymentRequestedConsumer(
    ISender sender,
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

        await sender.Send(
            new RefundPaymentCommand(message.PaymentId),
            context.CancellationToken);
    }
}
