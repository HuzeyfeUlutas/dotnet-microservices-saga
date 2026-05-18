namespace Order.Application.Contracts.IntegrationEvents;

public sealed record OrderLineSnapshotIntegrationItem(
    Guid OrderLineId,
    Guid ProductId,
    string Sku,
    string ProductName,
    string VariantName,
    decimal UnitPrice,
    string Currency,
    int Quantity,
    decimal LineTotal);
