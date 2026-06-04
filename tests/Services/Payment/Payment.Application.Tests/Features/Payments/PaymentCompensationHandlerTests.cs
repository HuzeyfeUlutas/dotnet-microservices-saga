using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Payment.Application.Abstractions.Providers;
using Payment.Application.Common.Exceptions;
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
    public async Task VoidAuthorization_should_throw_conflict_when_payment_save_fails()
    {
        await using var context = CreateConcurrencyFailingContext();
        var payment = CreatePayment("idem-void-conflict");
        payment.StartAuthorization("idem-void-conflict-auth");
        payment.RequireAction("provider-pay-1", "/fake-3ds/payments/1");
        payment.MarkAsAuthorized("provider-pay-1", "provider-auth-1");
        context.Payments.Add(payment);
        await context.SaveChangesAsync();
        context.FailNextSave = true;
        var provider = Substitute.For<IPaymentProvider>();
        provider.VoidAuthorizationAsync(payment, Arg.Any<CancellationToken>())
            .Returns(new ProviderPaymentResultDto(true, "provider-pay-1", "provider-void-1"));
        var handler = new VoidPaymentAuthorizationHandler(context, provider);

        var action = async () => await handler.Handle(
            new VoidPaymentAuthorizationCommand(Guid.NewGuid(), payment.Id, "void-conflict-request"),
            CancellationToken.None);

        await action.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task CancelPending_should_throw_conflict_when_payment_save_fails()
    {
        await using var context = CreateConcurrencyFailingContext();
        var payment = CreatePayment("idem-cancel-conflict");
        payment.StartAuthorization("idem-cancel-conflict-auth");
        payment.RequireAction("provider-pay-1", "/fake-3ds/payments/1");
        context.Payments.Add(payment);
        await context.SaveChangesAsync();
        context.FailNextSave = true;
        var handler = new CancelPendingPaymentHandler(context);

        var action = async () => await handler.Handle(
            new CancelPendingPaymentCommand(
                Guid.NewGuid(),
                payment.Id,
                "cancel-conflict-request",
                "Payment timeout expired."),
            CancellationToken.None);

        await action.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task VoidAuthorization_should_return_success_result()
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
        var handler = new VoidPaymentAuthorizationHandler(context, provider);
        var requestEventId = Guid.NewGuid();
        const string idempotencyKey = "void-success-request";

        var result = await handler.Handle(
            new VoidPaymentAuthorizationCommand(requestEventId, payment.Id, idempotencyKey),
            CancellationToken.None);

        result.Status.Should().Be(PaymentStatus.AuthorizationVoided);
        (await context.PaymentAttempts.CountAsync()).Should().Be(2);
        payment.Attempts.Single(x => x.Type == PaymentAttemptType.AuthorizationVoid)
            .IdempotencyKey.Should().Be(idempotencyKey);
    }

    [Fact]
    public async Task CancelPending_should_return_success_result()
    {
        var factory = new PaymentTestDbContextFactory();
        await using var context = factory.CreateContext();
        var payment = CreatePayment("idem-cancel");
        payment.StartAuthorization("idem-cancel-auth");
        payment.RequireAction("provider-pay-1", "/fake-3ds/payments/1");
        context.Payments.Add(payment);
        await context.SaveChangesAsync();
        var handler = new CancelPendingPaymentHandler(context);
        var requestEventId = Guid.NewGuid();

        var result = await handler.Handle(
            new CancelPendingPaymentCommand(
                requestEventId,
                payment.Id,
                "cancel-success-request",
                "Payment timeout expired."),
            CancellationToken.None);

        result.Status.Should().Be(PaymentStatus.Cancelled);
        payment.Attempts.Single().Status.Should().Be(PaymentAttemptStatus.Cancelled);
    }

    [Fact]
    public async Task VoidAuthorization_should_return_success_for_idempotent_retry()
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
        var handler = new VoidPaymentAuthorizationHandler(context, provider);
        var requestEventId = Guid.NewGuid();

        var result = await handler.Handle(
            new VoidPaymentAuthorizationCommand(requestEventId, payment.Id, "void-retry-request"),
            CancellationToken.None);

        result.Status.Should().Be(PaymentStatus.AuthorizationVoided);
        await provider.DidNotReceiveWithAnyArgs().VoidAuthorizationAsync(default!, default);
    }

    [Fact]
    public async Task CancelPending_should_return_success_for_idempotent_retry()
    {
        var factory = new PaymentTestDbContextFactory();
        await using var context = factory.CreateContext();
        var payment = CreatePayment("idem-cancel-retry");
        payment.CancelPending("Payment timeout expired.");
        context.Payments.Add(payment);
        await context.SaveChangesAsync();
        var handler = new CancelPendingPaymentHandler(context);
        var requestEventId = Guid.NewGuid();

        var result = await handler.Handle(
            new CancelPendingPaymentCommand(
                requestEventId,
                payment.Id,
                "cancel-retry-request",
                "Payment timeout expired."),
            CancellationToken.None);

        result.Status.Should().Be(PaymentStatus.Cancelled);
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

    private static ConcurrencyFailingPaymentDbContext CreateConcurrencyFailingContext()
    {
        var options = new DbContextOptionsBuilder<Payment.Persistence.Context.PaymentDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new ConcurrencyFailingPaymentDbContext(options);
    }
}
