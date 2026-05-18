using FluentAssertions;
using Payment.Domain.Entities;
using Payment.Domain.Enums;
using Payment.Domain.Exceptions;
using Payment.Domain.ValueObjects;
using Xunit;

namespace Payment.Domain.Tests;

public class PaymentTests
{
    [Fact]
    public void Authorization_flow_should_move_payment_to_authorized()
    {
        var payment = CreatePayment();

        payment.StartAuthorization("idem-1");
        payment.RequireAction("provider-pay-1", "/fake-3ds/payments/1");
        payment.MarkAsAuthorized("provider-pay-1", "provider-auth-1");

        payment.Status.Should().Be(PaymentStatus.Authorized);
        payment.ProviderPaymentId.Should().Be("provider-pay-1");
        payment.ProviderTransactionId.Should().Be("provider-auth-1");
        payment.AuthorizedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void Start_capture_should_throw_when_payment_is_not_authorized()
    {
        var payment = CreatePayment();

        var act = () => payment.StartCapture();

        act.Should().Throw<DomainException>()
            .WithMessage("Capture can only be started for an authorized payment.");
    }

    [Fact]
    public void Start_authorization_should_reject_when_active_attempt_exists()
    {
        var payment = CreatePayment();
        payment.StartAuthorization("idem-1");

        var act = () => payment.StartAuthorization("idem-2");

        act.Should().Throw<DomainException>()
            .WithMessage("Payment already has an active attempt.");
    }

    private static Payment.Domain.Entities.Payment CreatePayment()
    {
        return new Payment.Domain.Entities.Payment(
            Guid.NewGuid(),
            new Money(120.50m, "TRY"),
            PaymentProviderType.Fake,
            PaymentMethodType.Card,
            "payment-idem-1");
    }
}
