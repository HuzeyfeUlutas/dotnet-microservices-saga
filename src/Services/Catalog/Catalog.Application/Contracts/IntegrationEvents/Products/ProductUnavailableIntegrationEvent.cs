namespace Catalog.Application.Contracts.IntegrationEvents.Products;

public sealed record ProductUnavailableIntegrationEvent(
    Guid EventId,
    DateTime OccurredAtUtc,
    Guid ProductId,
    string Reason);
