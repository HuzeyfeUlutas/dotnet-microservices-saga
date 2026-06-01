using FluentAssertions;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Marketplace.Grpc.Inventory.V1;
using Order.Application.Abstractions.Services;
using Order.Application.Common.Exceptions;
using Order.Infrastructure.Configuration;
using Order.Infrastructure.Services;
using Xunit;

namespace Order.Infrastructure.Tests.Services;

public class InventoryReservationClientTests
{
    [Fact]
    public async Task ReserveOrderStockAsync_ShouldMapGrpcRequestAndResponse()
    {
        var orderId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var reservationId = Guid.NewGuid();
        var expiresAtUtc = DateTime.UtcNow.AddMinutes(15);
        var response = new ReserveOrderStockResponse();
        response.Items.Add(new ReservedOrderStockItem
        {
            ReservationId = reservationId.ToString(),
            ProductId = productId.ToString(),
            Sku = "SKU-1",
            OrderId = orderId.ToString(),
            Quantity = 2,
            Status = "Pending",
            ExpiresAtUtc = Timestamp.FromDateTime(expiresAtUtc.ToUniversalTime())
        });
        var grpcClient = new StubInventoryReservationGrpcClient
        {
            Response = response
        };
        var client = new InventoryReservationClient(
            grpcClient,
            new ServiceEndpointOptions
            {
                InventoryGrpcTimeoutSeconds = 3
            });

        var result = await client.ReserveOrderStockAsync(
            orderId,
            [new InventoryReservationItemDto(productId, "SKU-1", 2)],
            expiresAtUtc);

        result.Should().ContainSingle();
        result.Single().ReservationId.Should().Be(reservationId);
        grpcClient.Request.Should().NotBeNull();
        grpcClient.Request!.Items.Should().ContainSingle();
        grpcClient.Request.Items.Single().ProductId.Should().Be(productId.ToString());
        grpcClient.Deadline.Should().NotBeNull();
    }

    [Fact]
    public async Task ReserveOrderStockAsync_ShouldMapFailedPreconditionRpcStatus()
    {
        var grpcClient = new StubInventoryReservationGrpcClient
        {
            Exception = new RpcException(new Status(StatusCode.FailedPrecondition, "Insufficient stock."))
        };
        var client = new InventoryReservationClient(
            grpcClient,
            new ServiceEndpointOptions
            {
                InventoryGrpcTimeoutSeconds = 3
            });

        var action = async () => await client.ReserveOrderStockAsync(
            Guid.NewGuid(),
            [new InventoryReservationItemDto(Guid.NewGuid(), "SKU-1", 2)],
            DateTime.UtcNow.AddMinutes(15));

        await action.Should().ThrowAsync<ConflictException>()
            .WithMessage("Inventory reservation failed. Insufficient stock.");
    }

    private sealed class StubInventoryReservationGrpcClient : InventoryReservation.InventoryReservationClient
    {
        public ReserveOrderStockRequest? Request { get; private set; }
        public ReserveOrderStockResponse? Response { get; init; }
        public RpcException? Exception { get; init; }
        public DateTime? Deadline { get; private set; }

        public override AsyncUnaryCall<ReserveOrderStockResponse> ReserveOrderStockAsync(
            ReserveOrderStockRequest request,
            Metadata? headers = null,
            DateTime? deadline = null,
            CancellationToken cancellationToken = default)
        {
            Request = request;
            Deadline = deadline;

            return new AsyncUnaryCall<ReserveOrderStockResponse>(
                Exception is null
                    ? Task.FromResult(Response!)
                    : Task.FromException<ReserveOrderStockResponse>(Exception),
                Task.FromResult(new Metadata()),
                () => Status.DefaultSuccess,
                () => new Metadata(),
                () => { });
        }
    }
}
