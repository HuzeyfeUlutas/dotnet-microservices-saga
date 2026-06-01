using FluentAssertions;
using Marketplace.Contracts.Payment.V1;
using NSubstitute;
using Payment.Application.Abstractions.Messaging;
using Payment.Application.Abstractions.Providers;
using Payment.Application.DTOs;
using Payment.Application.Features.Payments.CancelPendingPayment;
using Payment.Application.Features.Payments.VoidPaymentAuthorization;
using Payment.Application.Tests.Support;
using Payment.Domain.Enums;
using Payment.Domain.ValueObjects;
using Xunit;

namespace Payment.Application.Tests.Features.Payments;

public class PaymentCompensationHandlerTests
{
    [Fact]
    public async Task VoidAuthorization_should_publish_success_result()
    {
        var factory = new PaymentTestDbContextFactory();
        await using var context = factory.CreateContext();
        var payment = CreatePayment("idem-void");
        payment.StartAuthorization("idem-void-auth");
        payment.RequireAction("provider-pay-1", "/fake-3ds/payments/1");
        payment.MarkAsAuthorized("provider-pay-1", "provider-auth-1");
        context.Payments.Add(payment);
        await context.SaveChangesAsync();
        var provider = Substitute.For<IPaymentProvider>();
        provider.VoidAuthorizationAsync(payment, Arg.Any<CancellationToken>())
            .Returns(new ProviderPaymentResultDto(true, "provider-pay-1", "provider-void-1"));
        var publisher = Substitute.For<IIntegrationEventPublisher>();
        var handler = new VoidPaymentAuthorizationHandler(context, provider, publisher);
        var requestEventId = Guid.NewGuid();

        var result = await handler.Handle(
            new VoidPaymentAuthorizationCommand(requestEventId, payment.Id),
            CancellationToken.None);

        result.Status.Should().Be(PaymentStatus.AuthorizationVoided);
        await publisher.Received(1).PublishAsync(
            Arg.Is<PaymentAuthorizationVoided>(message =>
                message.RequestEventId == requestEventId &&
                message.PaymentId == payment.Id &&
                message.OrderId == payment.OrderId),
            CancellationToken.None);
    }

    [Fact]
    public async Task CancelPending_should_publish_success_result()
    {
        var factory = new PaymentTestDbContextFactory();
        await using var context = factory.CreateContext();
        var payment = CreatePayment("idem-cancel");
        payment.StartAuthorization("idem-cancel-auth");
        payment.RequireAction("provider-pay-1", "/fake-3ds/payments/1");
        context.Payments.Add(payment);
        await context.SaveChangesAsync();
        var publisher = Substitute.For<IIntegrationEventPublisher>();
        var handler = new CancelPendingPaymentHandler(context, publisher);
        var requestEventId = Guid.NewGuid();

        var result = await handler.Handle(
            new CancelPendingPaymentCommand(requestEventId, payment.Id, "Payment timeout expired."),
            CancellationToken.None);

        result.Status.Should().Be(PaymentStatus.Cancelled);
        payment.Attempts.Single().Status.Should().Be(PaymentAttemptStatus.Cancelled);
        await publisher.Received(1).PublishAsync(
            Arg.Is<PaymentCancelled>(message =>
                message.RequestEventId == requestEventId &&
                message.PaymentId == payment.Id &&
                message.OrderId == payment.OrderId),
            CancellationToken.None);
    }

    [Fact]
    public async Task VoidAuthorization_should_republish_success_for_idempotent_retry()
    {
        var factory = new PaymentTestDbContextFactory();
        await using var context = factory.CreateContext();
        var payment = CreatePayment("idem-void-retry");
        payment.StartAuthorization("idem-void-retry-auth");
        payment.RequireAction("provider-pay-1", "/fake-3ds/payments/1");
        payment.MarkAsAuthorized("provider-pay-1", "provider-auth-1");
        payment.StartAuthorizationVoid("idem-void-retry-request");
        payment.MarkAuthorizationAsVoided("provider-void-1");
        context.Payments.Add(payment);
        await context.SaveChangesAsync();
        var provider = Substitute.For<IPaymentProvider>();
        var publisher = Substitute.For<IIntegrationEventPublisher>();
        var handler = new VoidPaymentAuthorizationHandler(context, provider, publisher);
        var requestEventId = Guid.NewGuid();

        var result = await handler.Handle(
            new VoidPaymentAuthorizationCommand(requestEventId, payment.Id),
            CancellationToken.None);

        result.Status.Should().Be(PaymentStatus.AuthorizationVoided);
        await provider.DidNotReceiveWithAnyArgs().VoidAuthorizationAsync(default!, default);
        await publisher.Received(1).PublishAsync(
            Arg.Is<PaymentAuthorizationVoided>(message => message.RequestEventId == requestEventId),
            CancellationToken.None);
    }

    [Fact]
    public async Task CancelPending_should_republish_success_for_idempotent_retry()
    {
        var factory = new PaymentTestDbContextFactory();
        await using var context = factory.CreateContext();
        var payment = CreatePayment("idem-cancel-retry");
        payment.CancelPending("Payment timeout expired.");
        context.Payments.Add(payment);
        await context.SaveChangesAsync();
        var publisher = Substitute.For<IIntegrationEventPublisher>();
        var handler = new CancelPendingPaymentHandler(context, publisher);
        var requestEventId = Guid.NewGuid();

        var result = await handler.Handle(
            new CancelPendingPaymentCommand(requestEventId, payment.Id, "Payment timeout expired."),
            CancellationToken.None);

        result.Status.Should().Be(PaymentStatus.Cancelled);
        await publisher.Received(1).PublishAsync(
            Arg.Is<PaymentCancelled>(message => message.RequestEventId == requestEventId),
            CancellationToken.None);
    }

    private static Payment.Domain.Entities.Payment CreatePayment(string idempotencyKey)
    {
        return new Payment.Domain.Entities.Payment(
            Guid.NewGuid(),
            new Money(300m, "TRY"),
            PaymentProviderType.Fake,
            PaymentMethodType.Card,
            idempotencyKey);
    }
}
