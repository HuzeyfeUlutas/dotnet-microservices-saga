using FluentAssertions;
using Marketplace.Contracts.Inventory.V1;
using Marketplace.Contracts.Payment.V1;
using MassTransit;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Order.Infrastructure.Messaging.Consumers;
using Order.Infrastructure.Tests.Support;
using Order.Persistence.Sagas;
using Xunit;

namespace Order.Infrastructure.Tests.Consumers;

public class CommittedStockReverseConsumerTests
{
    [Fact]
    public async Task CommittedStockReversed_should_wait_for_payment_authorization_void()
    {
        var factory = new OrderTestDbContextFactory();
        await using var context = factory.CreateContext();
        var orderId = Guid.NewGuid();
        context.OrderCheckoutSagaStates.Add(CreateSagaState(orderId));
        await context.SaveChangesAsync();
        var publishEndpoint = Substitute.For<IPublishEndpoint>();
        var consumer = new CommittedStockReversedConsumer(
            context,
            publishEndpoint,
            NullLogger<CommittedStockReversedConsumer>.Instance);
        var consumeContext = Substitute.For<ConsumeContext<CommittedStockReversed>>();
        consumeContext.Message.Returns(
            new CommittedStockReversed(Guid.NewGuid(), Guid.NewGuid(), orderId, DateTime.UtcNow));
        consumeContext.CancellationToken.Returns(CancellationToken.None);

        await consumer.Consume(consumeContext);

        await publishEndpoint.Received(1).Publish(
            Arg.Is<VoidPaymentAuthorizationRequested>(message =>
                message.OrderId == orderId &&
                message.PaymentId == context.OrderCheckoutSagaStates.Single().PaymentId),
            CancellationToken.None);
        context.OrderCheckoutSagaStates.Single().CurrentState.Should()
            .Be(OrderCheckoutSagaStatus.AuthorizationVoidRequestedAfterPaymentCaptureFailure);
    }

    [Fact]
    public async Task CommittedStockReverseFailed_should_require_manual_review()
    {
        var factory = new OrderTestDbContextFactory();
        await using var context = factory.CreateContext();
        var orderId = Guid.NewGuid();
        context.OrderCheckoutSagaStates.Add(CreateSagaState(orderId));
        await context.SaveChangesAsync();
        var consumer = new CommittedStockReverseFailedConsumer(
            context,
            NullLogger<CommittedStockReverseFailedConsumer>.Instance);
        var consumeContext = Substitute.For<ConsumeContext<CommittedStockReverseFailed>>();
        consumeContext.Message.Returns(
            new CommittedStockReverseFailed(Guid.NewGuid(), Guid.NewGuid(), orderId, "reverse failed", DateTime.UtcNow));
        consumeContext.CancellationToken.Returns(CancellationToken.None);

        await consumer.Consume(consumeContext);

        context.OrderCheckoutSagaStates.Single().CurrentState.Should()
            .Be(OrderCheckoutSagaStatus.ManualReviewRequired);
        context.OrderCheckoutSagaStates.Single().FailureReason.Should().Be("reverse failed");
    }

    private static OrderCheckoutSagaState CreateSagaState(Guid orderId)
    {
        return new OrderCheckoutSagaState
        {
            CorrelationId = Guid.NewGuid(),
            OrderId = orderId,
            PaymentId = Guid.NewGuid(),
            CurrentState = OrderCheckoutSagaStatus.StockReverseRequestedAfterPaymentCaptureFailure,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
    }
}
