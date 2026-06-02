using FluentAssertions;
using Marketplace.Contracts.Inventory.V1;
using MassTransit;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Order.Infrastructure.Messaging.Consumers;
using Order.Infrastructure.Tests.Support;
using Order.Persistence.Sagas;
using Xunit;

namespace Order.Infrastructure.Tests.Consumers;

public class StockFailureConsumerTests
{
    [Fact]
    public async Task StockReleaseFailed_should_require_manual_review_after_payment_timeout()
    {
        var factory = new OrderTestDbContextFactory();
        await using var context = factory.CreateContext();
        var orderId = Guid.NewGuid();
        context.OrderCheckoutSagaStates.Add(CreateSagaState(
            orderId,
            Guid.NewGuid(),
            OrderCheckoutSagaStatus.StockReleaseRequestedAfterPaymentTimeout));
        await context.SaveChangesAsync();

        var consumer = new StockReleaseFailedConsumer(
            context,
            NullLogger<StockReleaseFailedConsumer>.Instance);
        var consumeContext = Substitute.For<ConsumeContext<StockReleaseFailed>>();
        consumeContext.Message.Returns(
            new StockReleaseFailed(Guid.NewGuid(), Guid.NewGuid(), orderId, "release failed", DateTime.UtcNow));
        consumeContext.CancellationToken.Returns(CancellationToken.None);

        await consumer.Consume(consumeContext);

        context.OrderCheckoutSagaStates.Single().CurrentState.Should()
            .Be(OrderCheckoutSagaStatus.ManualReviewRequired);
        context.OrderCheckoutSagaStates.Single().FailureReason.Should().Be("release failed");
    }

    private static OrderCheckoutSagaState CreateSagaState(Guid orderId, Guid paymentId, string currentState)
    {
        return new OrderCheckoutSagaState
        {
            CorrelationId = orderId,
            OrderId = orderId,
            PaymentId = paymentId,
            CurrentState = currentState,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
    }
}
