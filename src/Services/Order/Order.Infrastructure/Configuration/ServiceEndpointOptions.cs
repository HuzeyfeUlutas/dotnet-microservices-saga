namespace Order.Infrastructure.Configuration;

public class ServiceEndpointOptions
{
    public const string SectionName = "ServiceEndpoints";

    public string CatalogBaseUrl { get; init; } = "http://localhost:5272";
    public string InventoryBaseUrl { get; init; } = "http://localhost:5273";
    public string PaymentBaseUrl { get; init; } = "http://localhost:5285";
}
