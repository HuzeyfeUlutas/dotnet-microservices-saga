namespace Catalog.Application.Contracts.IntegrationEvents.Products;

public sealed record ProductUnavailableIntegrationEvent(
    Guid EventId,
    DateTime OccurredAtUtc,
    Guid ProductId,
    string Reason,
    IReadOnlyCollection<ProductUnavailableVariantSnapshot> Variants);

public sealed record ProductUnavailableVariantSnapshot(
    Guid VariantId,
    string Sku);
