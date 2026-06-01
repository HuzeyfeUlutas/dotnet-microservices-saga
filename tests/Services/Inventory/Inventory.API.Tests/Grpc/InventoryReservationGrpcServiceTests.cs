using FluentAssertions;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Inventory.API.Grpc.Services;
using Inventory.Application.Features.Reservations.ReserveOrderStock;
using Marketplace.Grpc.Inventory.V1;
using MediatR;
using NSubstitute;
using Xunit;

namespace Inventory.API.Tests.Grpc;

public class InventoryReservationGrpcServiceTests
{
    [Fact]
    public async Task ReserveOrderStock_ShouldDelegateToApplicationCommandAndMapResponse()
    {
        var orderId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var reservationId = Guid.NewGuid();
        var expiresAtUtc = DateTime.UtcNow.AddMinutes(15);
        var sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<ReserveOrderStockCommand>(), Arg.Any<CancellationToken>())
            .Returns([
                new Inventory.Application.Features.Reservations.ReserveOrderStock.ReservedOrderStockItem(
                    reservationId,
                    productId,
                    "SKU-1",
                    orderId,
                    2,
                    "Pending",
                    expiresAtUtc)
            ]);
        var service = new InventoryReservationGrpcService(sender);
        var request = new ReserveOrderStockRequest
        {
            OrderId = orderId.ToString(),
            ExpiresAtUtc = Timestamp.FromDateTime(expiresAtUtc.ToUniversalTime())
        };
        request.Items.Add(new Marketplace.Grpc.Inventory.V1.ReserveOrderStockItem
        {
            ProductId = productId.ToString(),
            Sku = "SKU-1",
            Quantity = 2
        });

        var response = await service.ReserveOrderStock(request, Substitute.For<ServerCallContext>());

        response.Items.Should().ContainSingle();
        response.Items.Single().ReservationId.Should().Be(reservationId.ToString());
        await sender.Received(1).Send(
            Arg.Is<ReserveOrderStockCommand>(command =>
                command.OrderId == orderId &&
                command.Items.Count == 1 &&
                command.Items.Single().ProductId == productId),
            Arg.Any<CancellationToken>());
    }
}
