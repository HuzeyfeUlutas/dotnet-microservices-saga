namespace Order.API.Contracts.Checkout;

public sealed record CreateCheckoutRequest(
    Guid BuyerId,
    IReadOnlyCollection<CreateCheckoutItemRequest> Items,
    string IdempotencyKey,
    string Provider = "Fake",
    string Method = "Card");

public sealed record CreateCheckoutItemRequest(
    Guid ProductId,
    string Sku,
    int Quantity);
