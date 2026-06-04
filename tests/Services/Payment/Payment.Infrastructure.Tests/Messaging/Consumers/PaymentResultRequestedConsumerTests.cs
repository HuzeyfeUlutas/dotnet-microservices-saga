using Marketplace.Contracts.Payment.V1;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Payment.Application.Abstractions.Messaging;
using Payment.Application.DTOs;
using Payment.Application.Features.Payments.CapturePayment;
using Payment.Application.Features.Payments.RefundPayment;
using Payment.Domain.Enums;
using Payment.Infrastructure.Messaging.Consumers;
using Xunit;

namespace Payment.Infrastructure.Tests.Messaging.Consumers;

public class PaymentResultRequestedConsumerTests
{
    [Fact]
    public async Task Capture_should_publish_success_after_command_completes()
    {
        var sender = Substitute.For<ISender>();
        var publisher = Substitute.For<IIntegrationEventPublisher>();
        var consumer = new CapturePaymentRequestedConsumer(
            sender,
            publisher,
            NullLogger<CapturePaymentRequestedConsumer>.Instance);
        var message = new CapturePaymentRequested(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "capture-request",
            DateTime.UtcNow);
        var context = CreateContext(message);
        sender.Send(Arg.Any<CapturePaymentCommand>(), Arg.Any<CancellationToken>())
            .Returns(CreatePaymentDto(message.PaymentId, message.OrderId, PaymentStatus.Captured));

        await consumer.Consume(context);

        await sender.Received(1).Send(
            Arg.Is<CapturePaymentCommand>(command =>
                command.PaymentId == message.PaymentId &&
                command.IdempotencyKey == message.IdempotencyKey),
            CancellationToken.None);
        await publisher.Received(1).PublishAsync(
            Arg.Is<PaymentCaptured>(result =>
                result.PaymentId == message.PaymentId &&
                result.OrderId == message.OrderId),
            CancellationToken.None);
    }

    [Fact]
    public async Task Capture_should_publish_failure_after_command_completes()
    {
        var sender = Substitute.For<ISender>();
        var publisher = Substitute.For<IIntegrationEventPublisher>();
        var consumer = new CapturePaymentRequestedConsumer(
            sender,
            publisher,
            NullLogger<CapturePaymentRequestedConsumer>.Instance);
        var message = new CapturePaymentRequested(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "capture-failure-request",
            DateTime.UtcNow);
        var context = CreateContext(message);
        sender.Send(Arg.Any<CapturePaymentCommand>(), Arg.Any<CancellationToken>())
            .Returns(CreatePaymentDto(message.PaymentId, message.OrderId, PaymentStatus.CaptureFailed, "capture rejected"));

        await consumer.Consume(context);

        await publisher.Received(1).PublishAsync(
            Arg.Is<PaymentCaptureFailed>(result =>
                result.PaymentId == message.PaymentId &&
                result.OrderId == message.OrderId &&
                result.FailureReason == "capture rejected"),
            CancellationToken.None);
    }

    [Fact]
    public async Task Refund_should_publish_success_after_command_completes()
    {
        var sender = Substitute.For<ISender>();
        var publisher = Substitute.For<IIntegrationEventPublisher>();
        var consumer = new RefundPaymentRequestedConsumer(
            sender,
            publisher,
            NullLogger<RefundPaymentRequestedConsumer>.Instance);
        var message = new RefundPaymentRequested(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "refund-request",
            "checkout compensation",
            DateTime.UtcNow);
        var context = CreateContext(message);
        sender.Send(Arg.Any<RefundPaymentCommand>(), Arg.Any<CancellationToken>())
            .Returns(CreatePaymentDto(message.PaymentId, message.OrderId, PaymentStatus.Refunded));

        await consumer.Consume(context);

        await sender.Received(1).Send(
            Arg.Is<RefundPaymentCommand>(command =>
                command.PaymentId == message.PaymentId &&
                command.IdempotencyKey == message.IdempotencyKey),
            CancellationToken.None);
        await publisher.Received(1).PublishAsync(
            Arg.Is<PaymentRefunded>(result =>
                result.PaymentId == message.PaymentId &&
                result.OrderId == message.OrderId),
            CancellationToken.None);
    }

    [Fact]
    public async Task Refund_should_publish_failure_after_command_completes()
    {
        var sender = Substitute.For<ISender>();
        var publisher = Substitute.For<IIntegrationEventPublisher>();
        var consumer = new RefundPaymentRequestedConsumer(
            sender,
            publisher,
            NullLogger<RefundPaymentRequestedConsumer>.Instance);
        var message = new RefundPaymentRequested(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "refund-failure-request",
            "checkout compensation",
            DateTime.UtcNow);
        var context = CreateContext(message);
        sender.Send(Arg.Any<RefundPaymentCommand>(), Arg.Any<CancellationToken>())
            .Returns(CreatePaymentDto(message.PaymentId, message.OrderId, PaymentStatus.RefundFailed, "refund rejected"));

        await consumer.Consume(context);

        await publisher.Received(1).PublishAsync(
            Arg.Is<PaymentRefundFailed>(result =>
                result.PaymentId == message.PaymentId &&
                result.OrderId == message.OrderId &&
                result.FailureReason == "refund rejected"),
            CancellationToken.None);
    }

    private static ConsumeContext<TMessage> CreateContext<TMessage>(TMessage message)
        where TMessage : class
    {
        var context = Substitute.For<ConsumeContext<TMessage>>();
        context.Message.Returns(message);
        context.CancellationToken.Returns(CancellationToken.None);
        return context;
    }

    private static PaymentDto CreatePaymentDto(
        Guid paymentId,
        Guid orderId,
        PaymentStatus status,
        string? failureReason = null)
    {
        return new PaymentDto(
            paymentId,
            orderId,
            300m,
            "TRY",
            PaymentProviderType.Fake,
            PaymentMethodType.Card,
            status,
            "idem-payment",
            DateTime.UtcNow,
            null,
            null,
            null,
            failureReason);
    }
}
