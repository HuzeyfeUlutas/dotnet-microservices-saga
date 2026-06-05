namespace Order.API.Contracts.Checkout;

public sealed record CreateCheckoutRequest(
    IReadOnlyCollection<CreateCheckoutItemRequest> Items,
    string IdempotencyKey,
    string Provider = "Fake",
    string Method = "Card",
    Guid? BuyerId = null);

public sealed record CreateCheckoutItemRequest(
    Guid ProductId,
    string Sku,
    int Quantity);
