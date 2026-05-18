namespace Order.Application.DTOs;

public sealed record OrderLineDto(
    Guid OrderLineId,
    Guid ProductId,
    string Sku,
    string ProductName,
    string VariantName,
    decimal UnitPrice,
    string Currency,
    int Quantity,
    decimal LineTotal);
