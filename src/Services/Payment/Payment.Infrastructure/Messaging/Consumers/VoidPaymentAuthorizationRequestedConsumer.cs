using FluentValidation;
using Marketplace.Contracts.Payment.V1;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using Payment.Application.Abstractions.Messaging;
using Payment.Application.Common.Exceptions;
using Payment.Application.Features.Payments.VoidPaymentAuthorization;
using Payment.Domain.Enums;
using Payment.Domain.Exceptions;

namespace Payment.Infrastructure.Messaging.Consumers;

public sealed class VoidPaymentAuthorizationRequestedConsumer(
    ISender sender,
    IIntegrationEventPublisher integrationEventPublisher,
    ILogger<VoidPaymentAuthorizationRequestedConsumer> logger) : IConsumer<VoidPaymentAuthorizationRequested>
{
    public async Task Consume(ConsumeContext<VoidPaymentAuthorizationRequested> context)
    {
        var message = context.Message;

        logger.LogInformation(
            "Consuming payment authorization void request event {EventId} for payment {PaymentId} and order {OrderId}",
            message.EventId,
            message.PaymentId,
            message.OrderId);

        try
        {
            var payment = await sender.Send(
                new VoidPaymentAuthorizationCommand(message.EventId, message.PaymentId, message.IdempotencyKey),
                context.CancellationToken);

            if (payment.Status == PaymentStatus.AuthorizationVoided)
            {
                await integrationEventPublisher.PublishAsync(
                    new PaymentAuthorizationVoided(
                        Guid.NewGuid(),
                        message.EventId,
                        payment.Id,
                        payment.OrderId,
                        DateTime.UtcNow),
                    context.CancellationToken);
                return;
            }

            await integrationEventPublisher.PublishAsync(
                new PaymentAuthorizationVoidFailed(
                    Guid.NewGuid(),
                    message.EventId,
                    payment.Id,
                    payment.OrderId,
                    payment.FailureReason ?? "Payment authorization void failed.",
                    DateTime.UtcNow),
                context.CancellationToken);
        }
        catch (Exception exception) when (IsExpectedFailure(exception))
        {
            logger.LogWarning(
                exception,
                "Payment authorization void request event {EventId} failed for payment {PaymentId}",
                message.EventId,
                message.PaymentId);

            await integrationEventPublisher.PublishAsync(
                new PaymentAuthorizationVoidFailed(
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
