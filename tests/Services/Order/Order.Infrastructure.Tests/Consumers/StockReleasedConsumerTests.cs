using FluentAssertions;
using Marketplace.Contracts.Inventory.V1;
using MassTransit;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Order.Application.Abstractions.Messaging;
using Order.Application.Contracts.IntegrationEvents;
using Order.Domain.Enums;
using Order.Domain.ValueObjects;
using Order.Infrastructure.Messaging.Consumers;
using Order.Infrastructure.Tests.Support;
using Payment.Application.Contracts.IntegrationEvents;
using Xunit;

namespace Order.Infrastructure.Tests.Consumers;

public class StockReleasedConsumerTests
{
    [Fact]
    public async Task Consume_should_fail_payment_after_stock_release()
    {
        var factory = new OrderTestDbContextFactory();
        await using var context = factory.CreateContext();
        var order = new Order.Domain.Entities.Order(
            Guid.NewGuid(),
            "idem-stock-released",
            [new OrderLineSnapshot(Guid.NewGuid(), "SKU-1", "Product 1", "Variant 1", new Money(100m, "TRY"), 1)]);
        context.Orders.Add(order);
        await context.SaveChangesAsync();

        var publishEndpoint = Substitute.For<IPublishEndpoint>();
        var paymentId = Guid.NewGuid();
        var paymentFailedConsumer = new PaymentAuthorizationFailedConsumer(
            context,
            publishEndpoint,
            NullLogger<PaymentAuthorizationFailedConsumer>.Instance);
        var paymentFailedContext = Substitute.For<ConsumeContext<PaymentAuthorizationFailed>>();
        paymentFailedContext.Message.Returns(
            new PaymentAuthorizationFailed(Guid.NewGuid(), paymentId, order.Id, order.TotalAmount, order.Currency, "3ds failed", DateTime.UtcNow));
        paymentFailedContext.CancellationToken.Returns(CancellationToken.None);
        await paymentFailedConsumer.Consume(paymentFailedContext);

        var integrationEventPublisher = Substitute.For<IIntegrationEventPublisher>();
        var consumer = new StockReleasedConsumer(
            context,
            integrationEventPublisher,
            NullLogger<StockReleasedConsumer>.Instance);
        var consumeContext = Substitute.For<ConsumeContext<StockReleased>>();
        consumeContext.Message.Returns(new StockReleased(Guid.NewGuid(), Guid.NewGuid(), order.Id, DateTime.UtcNow));
        consumeContext.CancellationToken.Returns(CancellationToken.None);

        await consumer.Consume(consumeContext);

        context.Orders.Single().Status.Should().Be(OrderStatus.PaymentFailed);
        await integrationEventPublisher.Received(1)
            .PublishAsync(
                Arg.Is<OrderPaymentFailed>(message =>
                    message.OrderId == order.Id &&
                    message.PaymentId == paymentId &&
                    message.FailureReason == "3ds failed"),
                CancellationToken.None);
    }
}
