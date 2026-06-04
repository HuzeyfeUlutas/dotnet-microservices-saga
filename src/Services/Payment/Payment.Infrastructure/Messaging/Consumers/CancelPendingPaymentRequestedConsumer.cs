using FluentValidation;
using Marketplace.Contracts.Payment.V1;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using Payment.Application.Abstractions.Messaging;
using Payment.Application.Common.Exceptions;
using Payment.Application.Features.Payments.CancelPendingPayment;
using Payment.Domain.Exceptions;

namespace Payment.Infrastructure.Messaging.Consumers;

public sealed class CancelPendingPaymentRequestedConsumer(
    ISender sender,
    IIntegrationEventPublisher integrationEventPublisher,
    ILogger<CancelPendingPaymentRequestedConsumer> logger) : IConsumer<CancelPendingPaymentRequested>
{
    public async Task Consume(ConsumeContext<CancelPendingPaymentRequested> context)
    {
        var message = context.Message;

        logger.LogInformation(
            "Consuming pending payment cancellation request event {EventId} for payment {PaymentId} and order {OrderId}",
            message.EventId,
            message.PaymentId,
            message.OrderId);

        try
        {
            var payment = await sender.Send(
                new CancelPendingPaymentCommand(
                    message.EventId,
                    message.PaymentId,
                    message.IdempotencyKey,
                    message.Reason),
                context.CancellationToken);

            await integrationEventPublisher.PublishAsync(
                new PaymentCancelled(
                    Guid.NewGuid(),
                    message.EventId,
                    payment.Id,
                    payment.OrderId,
                    DateTime.UtcNow),
                context.CancellationToken);
        }
        catch (Exception exception) when (IsExpectedFailure(exception))
        {
            logger.LogWarning(
                exception,
                "Pending payment cancellation request event {EventId} failed for payment {PaymentId}",
                message.EventId,
                message.PaymentId);

            await integrationEventPublisher.PublishAsync(
                new PaymentCancellationFailed(
                    Guid.NewGuid(),
                    message.EventId,
                    message.PaymentId,
                    message.OrderId,
                    exception.Message,
                    DateTime.UtcNow),
                context.CancellationToken);
        }
    }

    private static bool IsExpectedFailure(Exception exception)
    {
        return exception is ValidationException or NotFoundException or ConflictException or DomainException;
    }
}
