using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Marketplace.Grpc.Inventory.V1;
using Order.Application.Abstractions.Services;
using Order.Application.Common.Exceptions;
using Order.Infrastructure.Configuration;

namespace Order.Infrastructure.Services;

internal sealed class InventoryReservationClient(
    InventoryReservation.InventoryReservationClient grpcClient,
    ServiceEndpointOptions options) : IInventoryReservationClient
{
    public async Task<IReadOnlyCollection<InventoryReservationResultDto>> ReserveOrderStockAsync(
        Guid orderId,
        IReadOnlyCollection<InventoryReservationItemDto> items,
        DateTime? expiresAtUtc,
        CancellationToken cancellationToken = default)
    {
        var request = new ReserveOrderStockRequest
        {
            OrderId = orderId.ToString()
        };

        if (expiresAtUtc.HasValue)
        {
            request.ExpiresAtUtc = Timestamp.FromDateTime(expiresAtUtc.Value.ToUniversalTime());
        }

        request.Items.AddRange(items.Select(item => new ReserveOrderStockItem
        {
            ProductId = item.ProductId.ToString(),
            Sku = item.Sku,
            Quantity = item.Quantity
        }));

        try
        {
            var response = await grpcClient.ReserveOrderStockAsync(
                request,
                deadline: DateTime.UtcNow.AddSeconds(options.InventoryGrpcTimeoutSeconds),
                cancellationToken: cancellationToken);

            return response.Items
                .Select(MapReservation)
                .ToList();
        }
        catch (RpcException exception) when (exception.StatusCode == StatusCode.NotFound)
        {
            throw new NotFoundException($"Inventory reservation failed. {exception.Status.Detail}");
        }
        catch (RpcException exception) when (
            exception.StatusCode is StatusCode.InvalidArgument or StatusCode.FailedPrecondition or StatusCode.Aborted)
        {
            throw new ConflictException($"Inventory reservation failed. {exception.Status.Detail}");
        }
        catch (RpcException exception)
        {
            throw new IntegrationException($"Inventory reservation gRPC request failed with status '{exception.StatusCode}'. {exception.Status.Detail}");
        }
    }

    private static InventoryReservationResultDto MapReservation(ReservedOrderStockItem reservation)
    {
        if (!Guid.TryParse(reservation.ReservationId, out var reservationId) ||
            !Guid.TryParse(reservation.ProductId, out var productId) ||
            !Guid.TryParse(reservation.OrderId, out var orderId))
        {
            throw new IntegrationException("Inventory reservation gRPC response contained invalid data.");
        }

        return new InventoryReservationResultDto(
            reservationId,
            productId,
            reservation.Sku,
            orderId,
            reservation.Quantity,
            reservation.Status,
            reservation.ExpiresAtUtc?.ToDateTime());
    }
}
