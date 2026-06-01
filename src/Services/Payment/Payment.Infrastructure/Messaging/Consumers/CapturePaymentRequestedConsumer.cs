using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using Marketplace.Contracts.Payment.V1;
using Payment.Application.Features.Payments.CapturePayment;

namespace Payment.Infrastructure.Messaging.Consumers;

public sealed class CapturePaymentRequestedConsumer(
    ISender sender,
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

        await sender.Send(
            new CapturePaymentCommand(message.PaymentId),
            context.CancellationToken);
    }
}
