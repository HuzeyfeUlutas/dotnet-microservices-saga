namespace Order.Application.DTOs;

public sealed record CheckoutPaymentActionDto(
    string Type,
    string? RedirectUrl = null,
    string? ClientSecret = null,
    string? HtmlContent = null);
