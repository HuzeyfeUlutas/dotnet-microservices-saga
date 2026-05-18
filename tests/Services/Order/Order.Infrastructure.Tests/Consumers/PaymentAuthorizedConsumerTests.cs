using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Order.Application.Abstractions.Services;
using Order.Domain.Enums;
using Order.Domain.ValueObjects;
using Order.Infrastructure.Messaging.Consumers;
using Order.Infrastructure.Tests.Support;
using Payment.Application.Contracts.IntegrationEvents;
using Xunit;

namespace Order.Infrastructure.Tests.Consumers;

public class PaymentAuthorizedConsumerTests
{
    [Fact]
    public async Task Consume_should_commit_reservations_and_publish_capture_request()
    {
        var factory = new OrderTestDbContextFactory();
        await using var context = factory.CreateContext();
        var order = new Order.Domain.Entities.Order(
            Guid.NewGuid(),
            "idem-auth",
            [
                new OrderLineSnapshot(Guid.NewGuid(), "SKU-1", "Product 1", "Variant 1", new Money(100m, "TRY"), 1)
            ]);
        context.Orders.Add(order);
        await context.SaveChangesAsync();

        var inventoryClient = Substitute.For<IInventoryReservationClient>();
        var publishEndpoint = Substitute.For<IPublishEndpoint>();
        var consumer = new PaymentAuthorizedConsumer(
            context,
            inventoryClient,
            publishEndpoint,
            NullLogger<PaymentAuthorizedConsumer>.Instance);
        var message = new PaymentAuthorized(Guid.NewGuid(), Guid.NewGuid(), order.Id, order.TotalAmount, order.Currency, DateTime.UtcNow);
        var consumeContext = Substitute.For<ConsumeContext<PaymentAuthorized>>();
        consumeContext.Message.Returns(message);
        consumeContext.CancellationToken.Returns(CancellationToken.None);

        await consumer.Consume(consumeContext);

        await inventoryClient.Received(1)
            .CommitAsync(order.Lines.Single().ProductId, order.Lines.Single().Sku, order.Id, CancellationToken.None);
        await publishEndpoint.Received(1)
            .Publish(Arg.Is<CapturePaymentRequested>(x => x.OrderId == order.Id && x.PaymentId == message.PaymentId), CancellationToken.None);
        context.OrderCheckoutSagaStates.Should().ContainSingle(x =>
            x.OrderId == order.Id && x.CurrentState == Order.Persistence.Sagas.OrderCheckoutSagaStatus.CaptureRequested);
    }
}
