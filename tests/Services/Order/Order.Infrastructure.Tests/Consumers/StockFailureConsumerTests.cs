using FluentAssertions;
using Marketplace.Contracts.Inventory.V1;
using Marketplace.Contracts.Payment.V1;
using MassTransit;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Order.Domain.ValueObjects;
using Order.Infrastructure.Messaging.Consumers;
using Order.Infrastructure.Tests.Support;
using Order.Persistence.Sagas;
using Xunit;

namespace Order.Infrastructure.Tests.Consumers;

public class StockFailureConsumerTests
{
    [Fact]
    public async Task StockCommitFailed_should_publish_release_request()
    {
        var factory = new OrderTestDbContextFactory();
        await using var context = factory.CreateContext();
        var order = CreateOrder("idem-stock-commit-failed");
        context.Orders.Add(order);
        await context.SaveChangesAsync();
        var publishEndpoint = Substitute.For<IPublishEndpoint>();

        var paymentAuthorizedConsumer = new PaymentAuthorizedConsumer(
            context,
            publishEndpoint,
            NullLogger<PaymentAuthorizedConsumer>.Instance);
        var paymentAuthorizedContext = Substitute.For<ConsumeContext<PaymentAuthorized>>();
        paymentAuthorizedContext.Message.Returns(
            new PaymentAuthorized(Guid.NewGuid(), Guid.NewGuid(), order.Id, order.TotalAmount, order.Currency, DateTime.UtcNow));
        paymentAuthorizedContext.CancellationToken.Returns(CancellationToken.None);
        await paymentAuthorizedConsumer.Consume(paymentAuthorizedContext);

        var consumer = new StockCommitFailedConsumer(
            context,
            publishEndpoint,
            NullLogger<StockCommitFailedConsumer>.Instance);
        var consumeContext = Substitute.For<ConsumeContext<StockCommitFailed>>();
        consumeContext.Message.Returns(
            new StockCommitFailed(Guid.NewGuid(), Guid.NewGuid(), order.Id, "commit failed", DateTime.UtcNow));
        consumeContext.CancellationToken.Returns(CancellationToken.None);

        await consumer.Consume(consumeContext);

        await publishEndpoint.Received(1)
            .Publish(
                Arg.Is<ReleaseStockRequested>(message => message.OrderId == order.Id),
                CancellationToken.None);
        context.OrderCheckoutSagaStates.Single().CurrentState.Should()
            .Be(OrderCheckoutSagaStatus.StockReleaseRequestedAfterStockCommitFailure);
    }

    [Fact]
    public async Task StockReleaseFailed_should_require_manual_review()
    {
        var factory = new OrderTestDbContextFactory();
        await using var context = factory.CreateContext();
        var order = CreateOrder("idem-stock-release-failed");
        context.Orders.Add(order);
        await context.SaveChangesAsync();
        var publishEndpoint = Substitute.For<IPublishEndpoint>();

        var paymentFailedConsumer = new PaymentAuthorizationFailedConsumer(
            context,
            publishEndpoint,
            NullLogger<PaymentAuthorizationFailedConsumer>.Instance);
        var paymentFailedContext = Substitute.For<ConsumeContext<PaymentAuthorizationFailed>>();
        paymentFailedContext.Message.Returns(
            new PaymentAuthorizationFailed(Guid.NewGuid(), Guid.NewGuid(), order.Id, order.TotalAmount, order.Currency, "3ds failed", DateTime.UtcNow));
        paymentFailedContext.CancellationToken.Returns(CancellationToken.None);
        await paymentFailedConsumer.Consume(paymentFailedContext);

        var consumer = new StockReleaseFailedConsumer(
            context,
            NullLogger<StockReleaseFailedConsumer>.Instance);
        var consumeContext = Substitute.For<ConsumeContext<StockReleaseFailed>>();
        consumeContext.Message.Returns(
            new StockReleaseFailed(Guid.NewGuid(), Guid.NewGuid(), order.Id, "release failed", DateTime.UtcNow));
        consumeContext.CancellationToken.Returns(CancellationToken.None);

        await consumer.Consume(consumeContext);

        context.OrderCheckoutSagaStates.Single().CurrentState.Should()
            .Be(OrderCheckoutSagaStatus.ManualReviewRequired);
        context.OrderCheckoutSagaStates.Single().FailureReason.Should().Be("release failed");
    }

    private static Order.Domain.Entities.Order CreateOrder(string idempotencyKey)
    {
        return new Order.Domain.Entities.Order(
            Guid.NewGuid(),
            idempotencyKey,
            [new OrderLineSnapshot(Guid.NewGuid(), "SKU-1", "Product 1", "Variant 1", new Money(100m, "TRY"), 1)]);
    }
}
