namespace Payment.Application.DTOs;

public sealed record PaymentActionDto(
    string Type,
    string? RedirectUrl = null,
    string? ClientSecret = null,
    string? HtmlContent = null);
