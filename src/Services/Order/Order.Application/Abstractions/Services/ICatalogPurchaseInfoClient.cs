namespace Order.Application.Abstractions.Services;

public interface ICatalogPurchaseInfoClient
{
    Task<CatalogPurchaseInfoDto> GetPurchaseInfoAsync(
        Guid productId,
        string sku,
        CancellationToken cancellationToken = default);
}
