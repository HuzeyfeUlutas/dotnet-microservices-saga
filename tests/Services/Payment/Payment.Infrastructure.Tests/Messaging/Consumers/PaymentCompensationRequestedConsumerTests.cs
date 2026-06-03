using Marketplace.Contracts.Payment.V1;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Payment.Application.Abstractions.Messaging;
using Payment.Application.DTOs;
using Payment.Application.Features.Payments.CancelPendingPayment;
using Payment.Application.Features.Payments.VoidPaymentAuthorization;
using Payment.Domain.Enums;
using Payment.Domain.Exceptions;
using Payment.Infrastructure.Messaging.Consumers;
using Xunit;

namespace Payment.Infrastructure.Tests.Messaging.Consumers;

public class PaymentCompensationRequestedConsumerTests
{
    [Fact]
    public async Task VoidAuthorization_should_dispatch_command_and_publish_success()
    {
        var sender = Substitute.For<ISender>();
        var publisher = Substitute.For<IIntegrationEventPublisher>();
        var consumer = new VoidPaymentAuthorizationRequestedConsumer(
            sender,
            publisher,
            NullLogger<VoidPaymentAuthorizationRequestedConsumer>.Instance);
        var message = new VoidPaymentAuthorizationRequested(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "capture failed",
            DateTime.UtcNow);
        var context = Substitute.For<ConsumeContext<VoidPaymentAuthorizationRequested>>();
        context.Message.Returns(message);
        context.CancellationToken.Returns(CancellationToken.None);
        sender.Send(Arg.Any<VoidPaymentAuthorizationCommand>(), Arg.Any<CancellationToken>())
            .Returns(CreatePaymentDto(message.PaymentId, message.OrderId, PaymentStatus.AuthorizationVoided));

        await consumer.Consume(context);

        await sender.Received(1).Send(
            Arg.Is<VoidPaymentAuthorizationCommand>(command =>
                command.RequestEventId == message.EventId &&
                command.PaymentId == message.PaymentId),
            CancellationToken.None);
        await publisher.Received(1).PublishAsync(
            Arg.Is<PaymentAuthorizationVoided>(result =>
                result.RequestEventId == message.EventId &&
                result.PaymentId == message.PaymentId &&
                result.OrderId == message.OrderId),
            CancellationToken.None);
    }

    [Fact]
    public async Task VoidAuthorization_should_publish_failure_for_domain_rejection()
    {
        var sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<VoidPaymentAuthorizationCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<PaymentDto>(new DomainException("Authorization void rejected.")));
        var publisher = Substitute.For<IIntegrationEventPublisher>();
        var consumer = new VoidPaymentAuthorizationRequestedConsumer(
            sender,
            publisher,
            NullLogger<VoidPaymentAuthorizationRequestedConsumer>.Instance);
        var message = new VoidPaymentAuthorizationRequested(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "capture failed",
            DateTime.UtcNow);
        var context = Substitute.For<ConsumeContext<VoidPaymentAuthorizationRequested>>();
        context.Message.Returns(message);
        context.CancellationToken.Returns(CancellationToken.None);

        await consumer.Consume(context);

        await publisher.Received(1).PublishAsync(
            Arg.Is<PaymentAuthorizationVoidFailed>(result =>
                result.RequestEventId == message.EventId &&
                result.PaymentId == message.PaymentId &&
                result.OrderId == message.OrderId &&
                result.FailureReason == "Authorization void rejected."),
            CancellationToken.None);
    }

    [Fact]
    public async Task CancelPending_should_dispatch_command_and_publish_success()
    {
        var sender = Substitute.For<ISender>();
        var publisher = Substitute.For<IIntegrationEventPublisher>();
        var consumer = new CancelPendingPaymentRequestedConsumer(
            sender,
            publisher,
            NullLogger<CancelPendingPaymentRequestedConsumer>.Instance);
        var message = new CancelPendingPaymentRequested(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Payment timeout expired.",
            DateTime.UtcNow);
        var context = Substitute.For<ConsumeContext<CancelPendingPaymentRequested>>();
        context.Message.Returns(message);
        context.CancellationToken.Returns(CancellationToken.None);
        sender.Send(Arg.Any<CancelPendingPaymentCommand>(), Arg.Any<CancellationToken>())
            .Returns(CreatePaymentDto(message.PaymentId, message.OrderId, PaymentStatus.Cancelled));

        await consumer.Consume(context);

        await sender.Received(1).Send(
            Arg.Is<CancelPendingPaymentCommand>(command =>
                command.RequestEventId == message.EventId &&
                command.PaymentId == message.PaymentId &&
                command.Reason == message.Reason),
            CancellationToken.None);
        await publisher.Received(1).PublishAsync(
            Arg.Is<PaymentCancelled>(result =>
                result.RequestEventId == message.EventId &&
                result.PaymentId == message.PaymentId &&
                result.OrderId == message.OrderId),
            CancellationToken.None);
    }

    [Fact]
    public async Task CancelPending_should_publish_failure_for_domain_rejection()
    {
        var sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<CancelPendingPaymentCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<PaymentDto>(new DomainException("Cancellation rejected.")));
        var publisher = Substitute.For<IIntegrationEventPublisher>();
        var consumer = new CancelPendingPaymentRequestedConsumer(
            sender,
            publisher,
            NullLogger<CancelPendingPaymentRequestedConsumer>.Instance);
        var message = new CancelPendingPaymentRequested(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Payment timeout expired.",
            DateTime.UtcNow);
        var context = Substitute.For<ConsumeContext<CancelPendingPaymentRequested>>();
        context.Message.Returns(message);
        context.CancellationToken.Returns(CancellationToken.None);

        await consumer.Consume(context);

        await publisher.Received(1).PublishAsync(
            Arg.Is<PaymentCancellationFailed>(result =>
                result.RequestEventId == message.EventId &&
                result.PaymentId == message.PaymentId &&
                result.OrderId == message.OrderId &&
                result.FailureReason == "Cancellation rejected."),
            CancellationToken.None);
    }

    private static PaymentDto CreatePaymentDto(Guid paymentId, Guid orderId, PaymentStatus status)
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
            null);
    }
}
