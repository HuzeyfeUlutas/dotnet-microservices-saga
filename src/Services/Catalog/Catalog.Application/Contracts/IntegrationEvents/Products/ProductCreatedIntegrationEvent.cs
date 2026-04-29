namespace Catalog.Application.Contracts.IntegrationEvents.Products;

public sealed record ProductCreatedIntegrationEvent(
    Guid EventId,
    DateTime OccurredAtUtc,
    Guid ProductId,
    string Name,
    decimal Price,
    Guid BrandId,
    Guid CategoryId);
