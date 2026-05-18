using System.Net;
using System.Net.Http.Json;
using Order.Application.Abstractions.Services;
using Order.Application.Common.Exceptions;

namespace Order.Infrastructure.Services;

internal sealed class CatalogPurchaseInfoClient(HttpClient httpClient) : ICatalogPurchaseInfoClient
{
    public async Task<CatalogPurchaseInfoDto> GetPurchaseInfoAsync(
        Guid productId,
        string sku,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetAsync($"/api/products/{productId}/purchase-info?sku={Uri.EscapeDataString(sku)}", cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            throw new NotFoundException($"Catalog purchase info for product '{productId}' and SKU '{sku}' was not found.");
        }

        if (!response.IsSuccessStatusCode)
        {
            var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new IntegrationException($"Catalog purchase info request failed with status {(int)response.StatusCode}. {responseText}");
        }

        var payload = await response.Content.ReadFromJsonAsync<CatalogPurchaseInfoResponse>(cancellationToken: cancellationToken)
                      ?? throw new IntegrationException("Catalog purchase info response was empty.");

        return new CatalogPurchaseInfoDto(
            payload.ProductId,
            payload.ProductName,
            payload.Sku,
            payload.VariantName,
            payload.UnitPrice,
            payload.Currency,
            payload.ProductStatus.ToString(),
            payload.VariantStatus.ToString(),
            payload.BrandId,
            payload.CategoryId,
            payload.IsPurchasable,
            payload.NotPurchasableReason);
    }

    private sealed record CatalogPurchaseInfoResponse(
        Guid ProductId,
        string ProductName,
        string Sku,
        string VariantName,
        decimal UnitPrice,
        string Currency,
        int ProductStatus,
        int VariantStatus,
        Guid BrandId,
        Guid CategoryId,
        bool IsPurchasable,
        string? NotPurchasableReason);
}
