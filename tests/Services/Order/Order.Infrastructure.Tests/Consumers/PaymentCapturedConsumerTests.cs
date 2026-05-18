using FluentAssertions;
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

public class PaymentCapturedConsumerTests
{
    [Fact]
    public async Task Consume_should_confirm_order_and_publish_order_confirmed_event()
    {
        var factory = new OrderTestDbContextFactory();
        await using var context = factory.CreateContext();
        var order = new Order.Domain.Entities.Order(
            Guid.NewGuid(),
            "idem-captured",
            [
                new OrderLineSnapshot(Guid.NewGuid(), "SKU-1", "Product 1", "Variant 1", new Money(100m, "TRY"), 1)
            ]);
        context.Orders.Add(order);
        await context.SaveChangesAsync();

        var publisher = Substitute.For<IIntegrationEventPublisher>();
        var consumer = new PaymentCapturedConsumer(
            context,
            publisher,
            NullLogger<PaymentCapturedConsumer>.Instance);
        var message = new PaymentCaptured(Guid.NewGuid(), Guid.NewGuid(), order.Id, order.TotalAmount, order.Currency, DateTime.UtcNow);
        var consumeContext = Substitute.For<ConsumeContext<PaymentCaptured>>();
        consumeContext.Message.Returns(message);
        consumeContext.CancellationToken.Returns(CancellationToken.None);

        await consumer.Consume(consumeContext);

        context.Orders.Single().Status.Should().Be(OrderStatus.Confirmed);
        await publisher.Received(1)
            .PublishAsync(
                Arg.Is<OrderConfirmed>(x => x.OrderId == order.Id && x.PaymentId == message.PaymentId),
                CancellationToken.None);
    }
}
