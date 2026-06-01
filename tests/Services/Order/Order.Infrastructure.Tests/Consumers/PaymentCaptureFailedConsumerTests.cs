using FluentAssertions;
using Marketplace.Contracts.Inventory.V1;
using MassTransit;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Order.Domain.Enums;
using Order.Domain.ValueObjects;
using Order.Infrastructure.Messaging.Consumers;
using Order.Infrastructure.Tests.Support;
using Order.Persistence.Sagas;
using Payment.Application.Contracts.IntegrationEvents;
using Xunit;

namespace Order.Infrastructure.Tests.Consumers;

public class PaymentCaptureFailedConsumerTests
{
    [Fact]
    public async Task Consume_should_request_committed_stock_reverse_before_failing_order()
    {
        var factory = new OrderTestDbContextFactory();
        await using var context = factory.CreateContext();
        var order = new Order.Domain.Entities.Order(
            Guid.NewGuid(),
            "idem-capture-failed",
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

        var stockCommittedConsumer = new StockCommittedConsumer(
            context,
            publishEndpoint,
            NullLogger<StockCommittedConsumer>.Instance);
        var stockCommittedContext = Substitute.For<ConsumeContext<StockCommitted>>();
        stockCommittedContext.Message.Returns(
            new StockCommitted(Guid.NewGuid(), Guid.NewGuid(), order.Id, DateTime.UtcNow));
        stockCommittedContext.CancellationToken.Returns(CancellationToken.None);
        await stockCommittedConsumer.Consume(stockCommittedContext);
        publishEndpoint.ClearReceivedCalls();

        var consumer = new PaymentCaptureFailedConsumer(
            context,
            publishEndpoint,
            NullLogger<PaymentCaptureFailedConsumer>.Instance);
        var consumeContext = Substitute.For<ConsumeContext<PaymentCaptureFailed>>();
        consumeContext.Message.Returns(
            new PaymentCaptureFailed(Guid.NewGuid(), paymentId, order.Id, order.TotalAmount, order.Currency, "capture failed", DateTime.UtcNow));
        consumeContext.CancellationToken.Returns(CancellationToken.None);

        await consumer.Consume(consumeContext);

        await publishEndpoint.Received(1)
            .Publish(
                Arg.Is<ReverseCommittedStockRequested>(message =>
                    message.OrderId == order.Id &&
                    message.Items.Count == 1),
                CancellationToken.None);
        context.Orders.Single().Status.Should().Be(OrderStatus.WaitingForPayment);
        context.OrderCheckoutSagaStates.Single().CurrentState.Should()
            .Be(OrderCheckoutSagaStatus.StockReverseRequestedAfterPaymentCaptureFailure);
    }
}
