using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Order.Application.Abstractions.Persistence;
using Order.Application.Abstractions.Services;
using Order.Application.Common.Exceptions;
using Order.Application.DTOs;
using Order.Domain.Entities;
using Order.Domain.Exceptions;
using Order.Domain.ValueObjects;

namespace Order.Application.Features.Checkout.CreateCheckout;

public class CreateCheckoutHandler(
    IOrderDbContext context,
    ICatalogPurchaseInfoClient catalogPurchaseInfoClient,
    IInventoryReservationClient inventoryReservationClient,
    IPaymentClient paymentClient,
    ILogger<CreateCheckoutHandler> logger) : IRequestHandler<CreateCheckoutCommand, CheckoutResultDto>
{
    private static readonly TimeSpan ReservationTtl = TimeSpan.FromMinutes(15);

    public async Task<CheckoutResultDto> Handle(CreateCheckoutCommand request, CancellationToken cancellationToken)
    {
        var existingOrder = await context.Orders
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.IdempotencyKey == request.IdempotencyKey, cancellationToken);

        if (existingOrder is not null)
        {
            EnsureExistingOrderMatchesRequest(existingOrder, request);

            if (existingOrder.Status != Order.Domain.Enums.OrderStatus.WaitingForPayment)
            {
                return new CheckoutResultDto(
                    existingOrder.Id,
                    existingOrder.Status,
                    new CheckoutPaymentDto(
                        existingOrder.PaymentId ?? Guid.Empty,
                        existingOrder.Status.ToString(),
                        request.Provider,
                        new CheckoutPaymentActionDto("None")));
            }

            var paymentResult = await paymentClient.CreatePaymentAsync(
                existingOrder.Id,
                existingOrder.TotalAmount,
                existingOrder.Currency,
                request.IdempotencyKey,
                request.Provider,
                request.Method,
                cancellationToken);

            if (!existingOrder.PaymentId.HasValue)
            {
                existingOrder.AttachPayment(paymentResult.PaymentId);
                await SaveChangesAsync(cancellationToken);
            }

            return new CheckoutResultDto(
                existingOrder.Id,
                existingOrder.Status,
                new CheckoutPaymentDto(
                    paymentResult.PaymentId,
                    paymentResult.Status,
                    paymentResult.Provider,
                    new CheckoutPaymentActionDto(
                        paymentResult.Action.Type,
                        paymentResult.Action.RedirectUrl,
                        paymentResult.Action.ClientSecret,
                        paymentResult.Action.HtmlContent)));
        }

        var purchaseInfos = await GetPurchaseInfosAsync(request, cancellationToken);
        var lineSnapshots = CreateLineSnapshots(purchaseInfos, request.Items);
        var order = new Order.Domain.Entities.Order(request.BuyerId, request.IdempotencyKey, lineSnapshots);
        var reservationExpiresAtUtc = DateTime.UtcNow.Add(ReservationTtl);

        IReadOnlyCollection<CreateCheckoutItem> reservedItems = [];
        var orderPersisted = false;

        try
        {
            await inventoryReservationClient.ReserveOrderStockAsync(
                order.Id,
                request.Items
                    .Select(item => new InventoryReservationItemDto(item.ProductId, item.Sku, item.Quantity))
                    .ToList(),
                reservationExpiresAtUtc,
                cancellationToken);
            reservedItems = request.Items;

            context.Orders.Add(order);
            await SaveChangesAsync(cancellationToken);
            orderPersisted = true;

            var paymentResult = await paymentClient.CreatePaymentAsync(
                order.Id,
                order.TotalAmount,
                order.Currency,
                request.IdempotencyKey,
                request.Provider,
                request.Method,
                cancellationToken);

            order.AttachPayment(paymentResult.PaymentId);
            await SaveChangesAsync(cancellationToken);

            return new CheckoutResultDto(
                order.Id,
                order.Status,
                new CheckoutPaymentDto(
                    paymentResult.PaymentId,
                    paymentResult.Status,
                    paymentResult.Provider,
                    new CheckoutPaymentActionDto(
                        paymentResult.Action.Type,
                        paymentResult.Action.RedirectUrl,
                        paymentResult.Action.ClientSecret,
                        paymentResult.Action.HtmlContent)));
        }
        catch (ConflictException)
        {
            if (!orderPersisted)
            {
                await ReleaseReservationsAsync(order.Id, reservedItems, cancellationToken);
            }

            throw;
        }
        catch (DomainException exception) when (order.PaymentId.HasValue)
        {
            throw new ConflictException(exception.Message);
        }
        catch (DomainException exception)
        {
            if (!orderPersisted)
            {
                await ReleaseReservationsAsync(order.Id, reservedItems, cancellationToken);
            }

            throw new ConflictException(exception.Message);
        }
        catch when (order.PaymentId.HasValue)
        {
            throw;
        }
        catch
        {
            if (!orderPersisted)
            {
                await ReleaseReservationsAsync(order.Id, reservedItems, cancellationToken);
            }

            throw;
        }
    }

    private async Task<List<CatalogPurchaseInfoDto>> GetPurchaseInfosAsync(
        CreateCheckoutCommand request,
        CancellationToken cancellationToken)
    {
        var purchaseInfos = new List<CatalogPurchaseInfoDto>(request.Items.Count);

        foreach (var item in request.Items)
        {
            var purchaseInfo = await catalogPurchaseInfoClient.GetPurchaseInfoAsync(
                item.ProductId,
                item.Sku,
                cancellationToken);

            if (!purchaseInfo.IsPurchasable)
            {
                throw new ConflictException(
                    purchaseInfo.NotPurchasableReason
                    ?? $"Product '{purchaseInfo.ProductId}' with SKU '{purchaseInfo.Sku}' is not purchasable.");
            }

            purchaseInfos.Add(purchaseInfo);
        }

        return purchaseInfos;
    }

    private static IReadOnlyCollection<OrderLineSnapshot> CreateLineSnapshots(
        IReadOnlyCollection<CatalogPurchaseInfoDto> purchaseInfos,
        IReadOnlyCollection<CreateCheckoutItem> items)
    {
        return items.Select(item =>
            {
                var purchaseInfo = purchaseInfos.First(x => x.ProductId == item.ProductId && x.Sku == item.Sku);

                return new OrderLineSnapshot(
                    purchaseInfo.ProductId,
                    purchaseInfo.Sku,
                    purchaseInfo.ProductName,
                    purchaseInfo.VariantName,
                    new Money(purchaseInfo.UnitPrice, purchaseInfo.Currency),
                    item.Quantity);
            })
            .ToList();
    }

    private async Task ReleaseReservationsAsync(
        Guid orderId,
        IReadOnlyCollection<CreateCheckoutItem> reservedItems,
        CancellationToken cancellationToken)
    {
        foreach (var reservedItem in reservedItems)
        {
            try
            {
                await inventoryReservationClient.ReleaseAsync(
                    reservedItem.ProductId,
                    reservedItem.Sku,
                    orderId,
                    cancellationToken);
            }
            catch
            {
                logger.LogWarning(
                    "Failed to release reservation for order {OrderId}, product {ProductId}, sku {Sku} during checkout rollback.",
                    orderId,
                    reservedItem.ProductId,
                    reservedItem.Sku);
            }
        }
    }

    private static void EnsureExistingOrderMatchesRequest(Order.Domain.Entities.Order existingOrder, CreateCheckoutCommand request)
    {
        if (existingOrder.BuyerId != request.BuyerId)
        {
            throw new ConflictException(
                $"Checkout idempotency key '{request.IdempotencyKey}' is already used for a different buyer.");
        }

        if (existingOrder.Lines.Count != request.Items.Count)
        {
            throw new ConflictException(
                $"Checkout idempotency key '{request.IdempotencyKey}' is already used for a different item set.");
        }

        var existingLines = existingOrder.Lines
            .OrderBy(line => line.ProductId)
            .ThenBy(line => line.Sku, StringComparer.Ordinal)
            .Select(line => new { line.ProductId, line.Sku, line.Quantity })
            .ToList();

        var requestedLines = request.Items
            .OrderBy(item => item.ProductId)
            .ThenBy(item => item.Sku, StringComparer.Ordinal)
            .Select(item => new { item.ProductId, item.Sku, item.Quantity })
            .ToList();

        for (var index = 0; index < existingLines.Count; index++)
        {
            var existingLine = existingLines[index];
            var requestedLine = requestedLines[index];

            if (existingLine.ProductId != requestedLine.ProductId ||
                !string.Equals(existingLine.Sku, requestedLine.Sku, StringComparison.Ordinal) ||
                existingLine.Quantity != requestedLine.Quantity)
            {
                throw new ConflictException(
                    $"Checkout idempotency key '{request.IdempotencyKey}' is already used for a different item set.");
            }
        }
    }

    private async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new ConflictException($"Order could not be created due to a concurrency conflict. {exception.Message}");
        }
        catch (DbUpdateException exception)
        {
            throw new ConflictException($"Order could not be created. {exception.Message}");
        }
    }
}
