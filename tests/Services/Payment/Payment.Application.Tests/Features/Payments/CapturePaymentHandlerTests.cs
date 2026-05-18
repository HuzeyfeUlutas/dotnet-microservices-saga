using FluentAssertions;
using NSubstitute;
using Payment.Application.Abstractions.Messaging;
using Payment.Application.Abstractions.Providers;
using Payment.Application.Contracts.IntegrationEvents;
using Payment.Application.Features.Payments.CapturePayment;
using Payment.Application.Tests.Support;
using Payment.Domain.Enums;
using Payment.Domain.ValueObjects;
using Xunit;

namespace Payment.Application.Tests.Features.Payments;

public class CapturePaymentHandlerTests
{
    [Fact]
    public async Task Handle_should_return_current_payment_when_already_captured()
    {
        var factory = new PaymentTestDbContextFactory();
        await using var context = factory.CreateContext();
        var provider = Substitute.For<IPaymentProvider>();
        var publisher = Substitute.For<IIntegrationEventPublisher>();
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

        var handler = new CapturePaymentHandler(context, provider, publisher);

        var result = await handler.Handle(new CapturePaymentCommand(payment.Id), CancellationToken.None);

        result.Status.Should().Be(PaymentStatus.Captured);
        await provider.DidNotReceiveWithAnyArgs().CaptureAsync(default!, default);
        await publisher.DidNotReceiveWithAnyArgs().PublishAsync(Arg.Any<PaymentCaptured>(), default);
    }
}
