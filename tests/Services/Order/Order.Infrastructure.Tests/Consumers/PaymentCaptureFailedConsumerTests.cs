using FluentAssertions;
using Marketplace.Contracts.Inventory.V1;
using Marketplace.Contracts.Payment.V1;
using MassTransit;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Order.Domain.Enums;
using Order.Domain.ValueObjects;
using Order.Infrastructure.Messaging.Consumers;
using Order.Infrastructure.Tests.Support;
using Order.Persistence.Sagas;
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
        context.OrderCheckoutSagaStates.Add(new OrderCheckoutSagaState
        {
            CorrelationId = order.Id,
            OrderId = order.Id,
            PaymentId = paymentId,
            CurrentState = OrderCheckoutSagaStatus.CaptureRequested,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

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
