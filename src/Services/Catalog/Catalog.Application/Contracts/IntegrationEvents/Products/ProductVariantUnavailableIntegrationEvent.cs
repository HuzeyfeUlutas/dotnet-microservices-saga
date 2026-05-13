namespace Catalog.Application.Contracts.IntegrationEvents.Products;

public sealed record ProductVariantUnavailableIntegrationEvent(
    Guid EventId,
    DateTime OccurredAtUtc,
    Guid ProductId,
    Guid VariantId,
    string Sku,
    string Reason);
