using System.Net;
using System.Net.Http.Json;
using Order.Application.Abstractions.Services;
using Order.Application.Common.Exceptions;

namespace Order.Infrastructure.Services;

internal sealed class InventoryReservationClient(HttpClient httpClient) : IInventoryReservationClient
{
    public async Task<InventoryReservationResultDto> ReserveAsync(
        Guid productId,
        string sku,
        Guid orderId,
        int quantity,
        DateTime? expiresAtUtc,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "/api/reservations",
            new ReserveRequest(productId, sku, orderId, quantity, expiresAtUtc),
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            throw new NotFoundException($"Inventory item for product '{productId}' and SKU '{sku}' was not found.");
        }

        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            var conflictText = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new ConflictException($"Inventory reservation failed. {conflictText}");
        }

        if (!response.IsSuccessStatusCode)
        {
            var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new IntegrationException($"Inventory reserve request failed with status {(int)response.StatusCode}. {responseText}");
        }

        var payload = await response.Content.ReadFromJsonAsync<ReserveResponse>(cancellationToken: cancellationToken)
                      ?? throw new IntegrationException("Inventory reserve response was empty.");

        return new InventoryReservationResultDto(
            payload.ReservationId,
            payload.ProductId,
            payload.Sku,
            payload.OrderId,
            payload.Quantity,
            payload.Status.ToString(),
            payload.ExpiresAtUtc);
    }

    public async Task CommitAsync(
        Guid productId,
        string sku,
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "/api/reservations/commit",
            new ReservationCommandRequest(productId, sku, orderId),
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            throw new NotFoundException($"Reservation for order '{orderId}', product '{productId}', and SKU '{sku}' was not found.");
        }

        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            var conflictText = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new ConflictException($"Inventory reservation commit failed. {conflictText}");
        }

        if (!response.IsSuccessStatusCode)
        {
            var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new IntegrationException($"Inventory commit request failed with status {(int)response.StatusCode}. {responseText}");
        }
    }

    public async Task ReleaseAsync(
        Guid productId,
        string sku,
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "/api/reservations/release",
            new ReservationCommandRequest(productId, sku, orderId),
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return;
        }

        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            var conflictText = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new ConflictException($"Inventory reservation release failed. {conflictText}");
        }

        if (!response.IsSuccessStatusCode)
        {
            var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new IntegrationException($"Inventory release request failed with status {(int)response.StatusCode}. {responseText}");
        }
    }

    private sealed record ReserveRequest(
        Guid ProductId,
        string Sku,
        Guid OrderId,
        int Quantity,
        DateTime? ExpiresAtUtc);

    private sealed record ReservationCommandRequest(
        Guid ProductId,
        string Sku,
        Guid OrderId);

    private sealed record ReserveResponse(
        Guid ReservationId,
        Guid ProductId,
        string Sku,
        Guid OrderId,
        int Quantity,
        int Status,
        DateTime? ExpiresAtUtc);
}
