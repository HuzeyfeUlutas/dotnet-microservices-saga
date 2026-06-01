using Inventory.Application.Abstractions.Messaging;
using Inventory.Application.Features.Reservations.ReleaseOrderStock;
using Inventory.Infrastructure.Messaging.Consumers;
using Marketplace.Contracts.Inventory.V1;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Inventory.Infrastructure.Tests.Messaging.Consumers;

public class ReleaseStockRequestedConsumerTests
{
    [Fact]
    public async Task Consume_should_dispatch_batch_command_and_publish_success()
    {
        var sender = Substitute.For<ISender>();
        var publisher = Substitute.For<IIntegrationEventPublisher>();
        var consumer = new ReleaseStockRequestedConsumer(
            sender,
            publisher,
            NullLogger<ReleaseStockRequestedConsumer>.Instance);
        var message = new ReleaseStockRequested(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [new StockReservationItem(Guid.NewGuid(), "SKU-1")],
            DateTime.UtcNow);
        var context = Substitute.For<ConsumeContext<ReleaseStockRequested>>();
        context.Message.Returns(message);
        context.CancellationToken.Returns(CancellationToken.None);

        await consumer.Consume(context);

        await sender.Received(1).Send(
            Arg.Is<ReleaseOrderStockCommand>(command =>
                command.OrderId == message.OrderId &&
                command.Items.Count == 1 &&
                command.Items.Single().ProductId == message.Items.Single().ProductId),
            CancellationToken.None);
        await publisher.Received(1).PublishAsync(
            Arg.Is<StockReleased>(result =>
                result.RequestEventId == message.EventId &&
                result.OrderId == message.OrderId),
            CancellationToken.None);
    }
}
