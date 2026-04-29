namespace Catalog.Application.Contracts.IntegrationEvents.Products;

public sealed record ProductPriceUpdatedIntegrationEvent(
    Guid EventId,
    DateTime OccurredAtUtc,
    Guid ProductId,
    decimal OldPrice,
    decimal NewPrice);
