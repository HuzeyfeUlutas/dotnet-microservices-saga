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

    [Fact]
    public void Authorization_void_flow_should_move_payment_to_authorization_voided()
    {
        var payment = CreateAuthorizedPayment();

        payment.StartAuthorizationVoid("void-idem-1");
        payment.MarkAuthorizationAsVoided("provider-void-1");

        payment.Status.Should().Be(PaymentStatus.AuthorizationVoided);
        payment.AuthorizationVoidedAtUtc.Should().NotBeNull();
        payment.Attempts.Last().Type.Should().Be(PaymentAttemptType.AuthorizationVoid);
        payment.Attempts.Last().Status.Should().Be(PaymentAttemptStatus.Succeeded);
    }

    [Fact]
    public void Cancel_pending_should_close_active_authorization_attempt()
    {
        var payment = CreatePayment();
        payment.StartAuthorization("idem-cancel");
        payment.RequireAction("provider-pay-1", "/fake-3ds/payments/1");

        payment.CancelPending("Payment timeout expired.");

        payment.Status.Should().Be(PaymentStatus.Cancelled);
        payment.CancelledAtUtc.Should().NotBeNull();
        payment.Attempts.Single().Status.Should().Be(PaymentAttemptStatus.Cancelled);
    }

    [Fact]
    public void Cancel_pending_should_reject_authorized_payment()
    {
        var payment = CreateAuthorizedPayment();

        var act = () => payment.CancelPending("Payment timeout expired.");

        act.Should().Throw<DomainException>()
            .WithMessage("Only pending payment can be cancelled.");
    }

    private static Payment.Domain.Entities.Payment CreateAuthorizedPayment()
    {
        var payment = CreatePayment();
        payment.StartAuthorization("idem-authorized");
        payment.RequireAction("provider-pay-1", "/fake-3ds/payments/1");
        payment.MarkAsAuthorized("provider-pay-1", "provider-auth-1");
        return payment;
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
