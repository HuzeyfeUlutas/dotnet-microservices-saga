using FluentAssertions;
using Marketplace.Contracts.Inventory.V1;
using MassTransit;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Order.Domain.ValueObjects;
using Order.Infrastructure.Messaging.Consumers;
using Order.Infrastructure.Tests.Support;
using Payment.Application.Contracts.IntegrationEvents;
using Xunit;

namespace Order.Infrastructure.Tests.Consumers;

public class StockCommittedConsumerTests
{
    [Fact]
    public async Task Consume_should_publish_capture_request_after_stock_commit()
    {
        var factory = new OrderTestDbContextFactory();
        await using var context = factory.CreateContext();
        var order = new Order.Domain.Entities.Order(
            Guid.NewGuid(),
            "idem-stock-committed",
            [new OrderLineSnapshot(Guid.NewGuid(), "SKU-1", "Product 1", "Variant 1", new Money(100m, "TRY"), 1)]);
        context.Orders.Add(order);
        await context.SaveChangesAsync();

        var publishEndpoint = Substitute.For<IPublishEndpoint>();
        var paymentId = Guid.NewGuid();
        var paymentAuthorizedConsumer = new PaymentAuthorizedConsumer(
            context,
            publishEndpoint,
            NullLogger<PaymentAuthorizedConsumer>.Instance);
        var paymentAuthorizedContext = Substitute.For<ConsumeContext<PaymentAuthorized>>();
        paymentAuthorizedContext.Message.Returns(
            new PaymentAuthorized(Guid.NewGuid(), paymentId, order.Id, order.TotalAmount, order.Currency, DateTime.UtcNow));
        paymentAuthorizedContext.CancellationToken.Returns(CancellationToken.None);
        await paymentAuthorizedConsumer.Consume(paymentAuthorizedContext);

        var consumer = new StockCommittedConsumer(
            context,
            publishEndpoint,
            NullLogger<StockCommittedConsumer>.Instance);
        var consumeContext = Substitute.For<ConsumeContext<StockCommitted>>();
        consumeContext.Message.Returns(new StockCommitted(Guid.NewGuid(), Guid.NewGuid(), order.Id, DateTime.UtcNow));
        consumeContext.CancellationToken.Returns(CancellationToken.None);

        await consumer.Consume(consumeContext);

        await publishEndpoint.Received(1)
            .Publish(
                Arg.Is<CapturePaymentRequested>(message =>
                    message.OrderId == order.Id &&
                    message.PaymentId == paymentId),
                CancellationToken.None);
        context.OrderCheckoutSagaStates.Should().ContainSingle(state =>
            state.OrderId == order.Id &&
            state.CurrentState == Order.Persistence.Sagas.OrderCheckoutSagaStatus.CaptureRequested);
    }
}
