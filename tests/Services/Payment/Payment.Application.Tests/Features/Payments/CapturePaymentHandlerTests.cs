using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Payment.Application.Abstractions.Providers;
using Payment.Application.Common.Exceptions;
using Payment.Application.DTOs;
using Payment.Application.Features.Payments.CapturePayment;
using Payment.Application.Tests.Support;
using Payment.Domain.Enums;
using Payment.Domain.ValueObjects;
using Xunit;

namespace Payment.Application.Tests.Features.Payments;

public class CapturePaymentHandlerTests
{
    [Fact]
    public async Task Handle_should_throw_conflict_when_payment_save_fails()
    {
        await using var context = CreateConcurrencyFailingContext();
        var payment = CreateAuthorizedPayment("idem-capture-conflict");
        context.Payments.Add(payment);
        await context.SaveChangesAsync();
        context.FailNextSave = true;
        var provider = Substitute.For<IPaymentProvider>();
        provider.CaptureAsync(payment, Arg.Any<CancellationToken>())
            .Returns(new ProviderPaymentResultDto(true, "provider-pay-1", "provider-capture-1"));
        var handler = new CapturePaymentHandler(context, provider, NullLogger<CapturePaymentHandler>.Instance);

        var action = async () => await handler.Handle(
            new CapturePaymentCommand(payment.Id),
            CancellationToken.None);

        await action.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Handle_should_return_current_payment_when_already_captured()
    {
        var factory = new PaymentTestDbContextFactory();
        await using var context = factory.CreateContext();
        var provider = Substitute.For<IPaymentProvider>();
        var payment = new Payment.Domain.Entities.Payment(
            Guid.NewGuid(),
            new Money(300m, "TRY"),
            PaymentProviderType.Fake,
            PaymentMethodType.Card,
            "idem-capture");
        payment.StartAuthorization("idem-capture");
        payment.RequireAction("provider-pay-1", "/fake-3ds/payments/1");
        payment.MarkAsAuthorized("provider-pay-1", "provider-auth-1");
        payment.StartCapture();
        payment.MarkAsCaptured("provider-capture-1");
        context.Payments.Add(payment);
        await context.SaveChangesAsync();

        var handler = new CapturePaymentHandler(context, provider, NullLogger<CapturePaymentHandler>.Instance);

        var result = await handler.Handle(new CapturePaymentCommand(payment.Id), CancellationToken.None);

        result.Status.Should().Be(PaymentStatus.Captured);
        await provider.DidNotReceiveWithAnyArgs().CaptureAsync(default!, default);
    }

    [Fact]
    public async Task Handle_should_persist_new_capture_attempt()
    {
        var factory = new PaymentTestDbContextFactory();
        await using var context = factory.CreateContext();
        var payment = CreateAuthorizedPayment("idem-capture-success");
        context.Payments.Add(payment);
        await context.SaveChangesAsync();
        var provider = Substitute.For<IPaymentProvider>();
        provider.CaptureAsync(payment, Arg.Any<CancellationToken>())
            .Returns(new ProviderPaymentResultDto(true, "provider-pay-1", "provider-capture-1"));
        var handler = new CapturePaymentHandler(context, provider, NullLogger<CapturePaymentHandler>.Instance);

        var result = await handler.Handle(new CapturePaymentCommand(payment.Id), CancellationToken.None);

        result.Status.Should().Be(PaymentStatus.Captured);
        (await context.PaymentAttempts.CountAsync()).Should().Be(2);
    }

    private static ConcurrencyFailingPaymentDbContext CreateConcurrencyFailingContext()
    {
        var options = new DbContextOptionsBuilder<Payment.Persistence.Context.PaymentDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new ConcurrencyFailingPaymentDbContext(options);
    }

    private static Payment.Domain.Entities.Payment CreateAuthorizedPayment(string idempotencyKey)
    {
        var payment = new Payment.Domain.Entities.Payment(
            Guid.NewGuid(),
            new Money(300m, "TRY"),
            PaymentProviderType.Fake,
            PaymentMethodType.Card,
            idempotencyKey);
        payment.StartAuthorization(idempotencyKey);
        payment.RequireAction("provider-pay-1", "/fake-3ds/payments/1");
        payment.MarkAsAuthorized("provider-pay-1", "provider-auth-1");

        return payment;
    }
}
