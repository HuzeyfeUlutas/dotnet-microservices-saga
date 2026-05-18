namespace Order.Application.Abstractions.Services;

public sealed record PaymentActionDto(
    string Type,
    string? RedirectUrl = null,
    string? ClientSecret = null,
    string? HtmlContent = null);
