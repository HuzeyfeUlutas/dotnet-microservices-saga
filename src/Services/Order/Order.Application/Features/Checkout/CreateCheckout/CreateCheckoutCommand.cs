using MediatR;
using Order.Application.DTOs;

namespace Order.Application.Features.Checkout.CreateCheckout;

public sealed record CreateCheckoutCommand(
    Guid BuyerId,
    IReadOnlyCollection<CreateCheckoutItem> Items,
    string IdempotencyKey,
    string Provider = "Fake",
    string Method = "Card") : IRequest<CheckoutResultDto>;

public sealed record CreateCheckoutItem(
    Guid ProductId,
    string Sku,
    int Quantity);
