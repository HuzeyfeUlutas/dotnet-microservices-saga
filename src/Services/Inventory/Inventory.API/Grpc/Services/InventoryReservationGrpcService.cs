using FluentValidation;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Inventory.Application.Common.Exceptions;
using Inventory.Application.Features.Reservations.ReserveOrderStock;
using Inventory.Domain.Exceptions;
using Marketplace.Grpc.Inventory.V1;
using MediatR;

namespace Inventory.API.Grpc.Services;

public sealed class InventoryReservationGrpcService(ISender sender) : InventoryReservation.InventoryReservationBase
{
    public override async Task<ReserveOrderStockResponse> ReserveOrderStock(
        ReserveOrderStockRequest request,
        ServerCallContext context)
    {
        if (!Guid.TryParse(request.OrderId, out var orderId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Order id must be a valid GUID."));
        }

        var commandItems = request.Items
            .Select(MapItem)
            .ToList();

        try
        {
            var reservations = await sender.Send(
                new ReserveOrderStockCommand(
                    orderId,
                    commandItems,
                    request.ExpiresAtUtc?.ToDateTime()),
                context.CancellationToken);

            var response = new ReserveOrderStockResponse();
            response.Items.AddRange(reservations.Select(MapReservation));
            return response;
        }
        catch (ValidationException exception)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, exception.Message));
        }
        catch (NotFoundException exception)
        {
            throw new RpcException(new Status(StatusCode.NotFound, exception.Message));
        }
        catch (DomainException exception)
        {
            throw new RpcException(new Status(StatusCode.FailedPrecondition, exception.Message));
        }
        catch (ConflictException exception)
        {
            throw new RpcException(new Status(StatusCode.Aborted, exception.Message));
        }
    }

    private static Inventory.Application.Features.Reservations.ReserveOrderStock.ReserveOrderStockItem MapItem(
        Marketplace.Grpc.Inventory.V1.ReserveOrderStockItem item)
    {
        if (!Guid.TryParse(item.ProductId, out var productId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Product id must be a valid GUID."));
        }

        return new Inventory.Application.Features.Reservations.ReserveOrderStock.ReserveOrderStockItem(
            productId,
            item.Sku,
            item.Quantity);
    }

    private static Marketplace.Grpc.Inventory.V1.ReservedOrderStockItem MapReservation(
        Inventory.Application.Features.Reservations.ReserveOrderStock.ReservedOrderStockItem reservation)
    {
        var result = new Marketplace.Grpc.Inventory.V1.ReservedOrderStockItem
        {
            ReservationId = reservation.ReservationId.ToString(),
            ProductId = reservation.ProductId.ToString(),
            Sku = reservation.Sku,
            OrderId = reservation.OrderId.ToString(),
            Quantity = reservation.Quantity,
            Status = reservation.Status
        };

        if (reservation.ExpiresAtUtc.HasValue)
        {
            result.ExpiresAtUtc = Timestamp.FromDateTime(reservation.ExpiresAtUtc.Value.ToUniversalTime());
        }

        return result;
    }
}
