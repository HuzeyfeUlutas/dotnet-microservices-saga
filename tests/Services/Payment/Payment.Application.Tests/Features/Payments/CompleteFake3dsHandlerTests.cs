using FluentAssertions;
using Marketplace.Contracts.Payment.V1;
using NSubstitute;
using Payment.Application.Abstractions.Messaging;
using Payment.Application.Abstractions.Providers;
using Payment.Application.DTOs;
using Payment.Application.Features.Payments.CompleteFake3ds;
using Payment.Application.Tests.Support;
using Payment.Domain.Entities;
using Payment.Domain.Enums;
using Payment.Domain.ValueObjects;
using Xunit;

namespace Payment.Application.Tests.Features.Payments;

public class CompleteFake3dsHandlerTests
{
    [Fact]
    public async Task Handle_should_return_current_payment_when_provider_event_was_already_processed()
    {
        var factory = new PaymentTestDbContextFactory();
        await using var context = factory.CreateContext();
        var provider = Substitute.For<IPaymentProvider>();
        var publisher = Substitute.For<IIntegrationEventPublisher>();
        var payment = new Payment.Domain.Entities.Payment(
            Guid.NewGuid(),
            new Money(150m, "TRY"),
            PaymentProviderType.Fake,
            PaymentMethodType.Card,
            "idem-payment");
        payment.StartAuthorization("idem-payment");
        payment.RequireAction("provider-pay-1", "/fake-3ds/payments/1");

        context.Payments.Add(payment);
        context.ProcessedProviderCallbacks.Add(
            new ProcessedProviderCallback(
                payment.Id,
                payment.Provider,
                "provider-event-1"));
        await context.SaveChangesAsync();

        var handler = new CompleteFake3dsHandler(context, provider, publisher);

        var result = await handler.Handle(
            new CompleteFake3dsCommand(payment.Id, Approved: true, ProviderEventId: "provider-event-1"),
            CancellationToken.None);

        result.Status.Should().Be(PaymentStatus.RequiresAction);
        await provider.DidNotReceiveWithAnyArgs()
            .CompleteAuthorizationAsync(default!, default, default);
        await publisher.DidNotReceiveWithAnyArgs()
            .PublishAsync(Arg.Any<PaymentAuthorized>(), default);
    }
}
