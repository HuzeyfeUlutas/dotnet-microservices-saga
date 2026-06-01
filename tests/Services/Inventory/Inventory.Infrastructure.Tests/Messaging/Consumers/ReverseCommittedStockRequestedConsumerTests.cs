using Inventory.Application.Abstractions.Messaging;
using Inventory.Application.Features.Reservations.ReverseCommittedOrderStock;
using Inventory.Domain.Exceptions;
using Inventory.Infrastructure.Messaging.Consumers;
using Marketplace.Contracts.Inventory.V1;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Inventory.Infrastructure.Tests.Messaging.Consumers;

public class ReverseCommittedStockRequestedConsumerTests
{
    [Fact]
    public async Task Consume_should_dispatch_batch_command_and_publish_success()
    {
        var sender = Substitute.For<ISender>();
        var publisher = Substitute.For<IIntegrationEventPublisher>();
        var consumer = new ReverseCommittedStockRequestedConsumer(
            sender,
            publisher,
            NullLogger<ReverseCommittedStockRequestedConsumer>.Instance);
        var message = new ReverseCommittedStockRequested(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [new StockReservationItem(Guid.NewGuid(), "SKU-1")],
            DateTime.UtcNow);
        var context = Substitute.For<ConsumeContext<ReverseCommittedStockRequested>>();
        context.Message.Returns(message);
        context.CancellationToken.Returns(CancellationToken.None);

        await consumer.Consume(context);

        await sender.Received(1).Send(
            Arg.Is<ReverseCommittedOrderStockCommand>(command =>
                command.OrderId == message.OrderId &&
                command.Items.Count == 1 &&
                command.Items.Single().ProductId == message.Items.Single().ProductId),
            CancellationToken.None);
        await publisher.Received(1).PublishAsync(
            Arg.Is<CommittedStockReversed>(result =>
                result.RequestEventId == message.EventId &&
                result.OrderId == message.OrderId),
            CancellationToken.None);
    }

    [Fact]
    public async Task Consume_should_publish_failure_for_domain_rejection()
    {
        var sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<ReverseCommittedOrderStockCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new DomainException("Only confirmed reservation commit can be reversed.")));
        var publisher = Substitute.For<IIntegrationEventPublisher>();
        var consumer = new ReverseCommittedStockRequestedConsumer(
            sender,
            publisher,
            NullLogger<ReverseCommittedStockRequestedConsumer>.Instance);
        var message = new ReverseCommittedStockRequested(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [new StockReservationItem(Guid.NewGuid(), "SKU-1")],
            DateTime.UtcNow);
        var context = Substitute.For<ConsumeContext<ReverseCommittedStockRequested>>();
        context.Message.Returns(message);
        context.CancellationToken.Returns(CancellationToken.None);

        await consumer.Consume(context);

        await publisher.Received(1).PublishAsync(
            Arg.Is<CommittedStockReverseFailed>(result =>
                result.RequestEventId == message.EventId &&
                result.OrderId == message.OrderId &&
                result.FailureReason == "Only confirmed reservation commit can be reversed."),
            CancellationToken.None);
    }
}
