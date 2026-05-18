using FluentAssertions;
using NSubstitute;
using Payment.Application.Abstractions.Providers;
using Payment.Application.DTOs;
using Payment.Application.Features.Payments.CreatePayment;
using Payment.Application.Tests.Support;
using Payment.Domain.Entities;
using Payment.Domain.Enums;
using Payment.Domain.ValueObjects;
using Xunit;

namespace Payment.Application.Tests.Features.Payments;

public class CreatePaymentHandlerTests
{
    [Fact]
    public async Task Handle_should_return_existing_requires_action_payment_without_restarting_provider_authorization()
    {
        var factory = new PaymentTestDbContextFactory();
        await using var context = factory.CreateContext();
        var provider = Substitute.For<IPaymentProvider>();
        var payment = new Payment.Domain.Entities.Payment(
            Guid.NewGuid(),
            new Money(250m, "TRY"),
            PaymentProviderType.Fake,
            PaymentMethodType.Card,
            "idem-existing");
        payment.StartAuthorization("idem-existing");
        payment.RequireAction("provider-pay-1", "/fake-3ds/payments/existing");
        context.Payments.Add(payment);
        await context.SaveChangesAsync();

        var handler = new CreatePaymentHandler(context, provider);

        var result = await handler.Handle(
            new CreatePaymentCommand(
                payment.OrderId,
                payment.Amount.Amount,
                payment.Amount.Currency,
                payment.IdempotencyKey,
                payment.Provider,
                payment.Method),
            CancellationToken.None);

        result.Payment.Id.Should().Be(payment.Id);
        result.Action.Type.Should().Be("Redirect");
        result.Action.RedirectUrl.Should().Be("/fake-3ds/payments/existing");
        await provider.DidNotReceiveWithAnyArgs()
            .StartAuthorizationAsync(default!, default);
    }
}
