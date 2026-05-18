using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Order.Application.Abstractions.Messaging;
using Order.Application.Abstractions.Services;
using Order.Application.Contracts.IntegrationEvents;
using Order.Domain.Enums;
using Order.Domain.ValueObjects;
using Order.Infrastructure.Messaging.Consumers;
using Order.Infrastructure.Tests.Support;
using Payment.Application.Contracts.IntegrationEvents;
using Xunit;

namespace Order.Infrastructure.Tests.Consumers;

public class PaymentAuthorizationFailedConsumerTests
{
    [Fact]
    public async Task Consume_should_release_reservations_and_publish_payment_failed_event()
    {
        var factory = new OrderTestDbContextFactory();
        await using var context = factory.CreateContext();
        var order = new Order.Domain.Entities.Order(
            Guid.NewGuid(),
            "idem-failed",
            [
                new OrderLineSnapshot(Guid.NewGuid(), "SKU-1", "Product 1", "Variant 1", new Money(100m, "TRY"), 1)
            ]);
        context.Orders.Add(order);
        await context.SaveChangesAsync();

        var publisher = Substitute.For<IIntegrationEventPublisher>();
        var inventoryClient = Substitute.For<IInventoryReservationClient>();
        var consumer = new PaymentAuthorizationFailedConsumer(
            context,
            publisher,
            inventoryClient,
            NullLogger<PaymentAuthorizationFailedConsumer>.Instance);
        var message = new PaymentAuthorizationFailed(Guid.NewGuid(), Guid.NewGuid(), order.Id, order.TotalAmount, order.Currency, "3ds failed", DateTime.UtcNow);
        var consumeContext = Substitute.For<ConsumeContext<PaymentAuthorizationFailed>>();
        consumeContext.Message.Returns(message);
        consumeContext.CancellationToken.Returns(CancellationToken.None);

        await consumer.Consume(consumeContext);

        context.Orders.Single().Status.Should().Be(OrderStatus.PaymentFailed);
        await inventoryClient.Received(1)
            .ReleaseAsync(order.Lines.Single().ProductId, order.Lines.Single().Sku, order.Id, CancellationToken.None);
        await publisher.Received(1)
            .PublishAsync(
                Arg.Is<OrderPaymentFailed>(x => x.OrderId == order.Id && x.FailureReason == "3ds failed"),
                CancellationToken.None);
    }
}
