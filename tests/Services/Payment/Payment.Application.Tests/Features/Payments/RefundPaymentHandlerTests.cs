using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Payment.Application.Abstractions.Providers;
using Payment.Application.Common.Exceptions;
using Payment.Application.DTOs;
using Payment.Application.Features.Payments.RefundPayment;
using Payment.Application.Tests.Support;
using Payment.Domain.Enums;
using Payment.Domain.ValueObjects;
using Xunit;

namespace Payment.Application.Tests.Features.Payments;

public class RefundPaymentHandlerTests
{
    [Fact]
    public async Task Handle_should_throw_conflict_when_payment_save_fails()
    {
        await using var context = CreateConcurrencyFailingContext();
        var payment = CreateCapturedPayment();
        context.Payments.Add(payment);
        await context.SaveChangesAsync();
        context.FailNextSave = true;
        var provider = Substitute.For<IPaymentProvider>();
        provider.RefundAsync(payment, Arg.Any<CancellationToken>())
            .Returns(new ProviderPaymentResultDto(true, "provider-pay-1", "provider-refund-1"));
        var handler = new RefundPaymentHandler(context, provider);

        var action = async () => await handler.Handle(
            new RefundPaymentCommand(payment.Id),
            CancellationToken.None);

        await action.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Handle_should_persist_new_refund_attempt()
    {
        var factory = new PaymentTestDbContextFactory();
        await using var context = factory.CreateContext();
        var payment = CreateCapturedPayment();
        context.Payments.Add(payment);
        await context.SaveChangesAsync();
        var provider = Substitute.For<IPaymentProvider>();
        provider.RefundAsync(payment, Arg.Any<CancellationToken>())
            .Returns(new ProviderPaymentResultDto(true, "provider-pay-1", "provider-refund-1"));
        var handler = new RefundPaymentHandler(context, provider);

        var result = await handler.Handle(new RefundPaymentCommand(payment.Id), CancellationToken.None);

        result.Status.Should().Be(PaymentStatus.Refunded);
        (await context.PaymentAttempts.CountAsync()).Should().Be(3);
    }

    private static ConcurrencyFailingPaymentDbContext CreateConcurrencyFailingContext()
    {
        var options = new DbContextOptionsBuilder<Payment.Persistence.Context.PaymentDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new ConcurrencyFailingPaymentDbContext(options);
    }

    private static Payment.Domain.Entities.Payment CreateCapturedPayment()
    {
        var payment = new Payment.Domain.Entities.Payment(
            Guid.NewGuid(),
            new Money(300m, "TRY"),
            PaymentProviderType.Fake,
            PaymentMethodType.Card,
            "idem-refund-conflict");
        payment.StartAuthorization("idem-refund-conflict");
        payment.RequireAction("provider-pay-1", "/fake-3ds/payments/1");
        payment.MarkAsAuthorized("provider-pay-1", "provider-auth-1");
        payment.StartCapture();
        payment.MarkAsCaptured("provider-capture-1");

        return payment;
    }
}
