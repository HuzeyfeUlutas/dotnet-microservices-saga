using System.Net;
using System.Net.Http.Json;
using Order.Application.Abstractions.Services;
using Order.Application.Common.Exceptions;

namespace Order.Infrastructure.Services;

internal sealed class PaymentClient(HttpClient httpClient) : IPaymentClient
{
    public async Task<PaymentInitiationResultDto> CreatePaymentAsync(
        Guid orderId,
        decimal amount,
        string currency,
        string idempotencyKey,
        string provider,
        string method,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "/api/payments",
            new CreatePaymentRequest(
                orderId,
                amount,
                currency,
                idempotencyKey,
                MapProvider(provider),
                MapMethod(method)),
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            var conflictText = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new ConflictException($"Payment initiation failed. {conflictText}");
        }

        if (!response.IsSuccessStatusCode)
        {
            var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new IntegrationException($"Payment initiation request failed with status {(int)response.StatusCode}. {responseText}");
        }

        var payload = await response.Content.ReadFromJsonAsync<CreatePaymentResponse>(cancellationToken: cancellationToken)
                      ?? throw new IntegrationException("Payment initiation response was empty.");

        return new PaymentInitiationResultDto(
            payload.Payment.Id,
            MapPaymentStatus(payload.Payment.Status),
            MapPaymentProvider(payload.Payment.Provider),
            new PaymentActionDto(
                payload.Action.Type,
                payload.Action.RedirectUrl,
                payload.Action.ClientSecret,
                payload.Action.HtmlContent));
    }

    private static int MapProvider(string provider)
    {
        return provider.Trim().Equals("Fake", StringComparison.OrdinalIgnoreCase) ? 1 : 1;
    }

    private static int MapMethod(string method)
    {
        return method.Trim().Equals("Card", StringComparison.OrdinalIgnoreCase) ? 1 : 1;
    }

    private static string MapPaymentStatus(int status)
    {
        return status switch
        {
            1 => "Pending",
            2 => "RequiresAction",
            3 => "Authorized",
            4 => "AuthorizationFailed",
            5 => "Captured",
            6 => "CaptureFailed",
            7 => "Refunded",
            8 => "RefundFailed",
            9 => "Cancelled",
            _ => status.ToString()
        };
    }

    private static string MapPaymentProvider(int provider)
    {
        return provider switch
        {
            1 => "Fake",
            _ => provider.ToString()
        };
    }

    private sealed record CreatePaymentRequest(
        Guid OrderId,
        decimal Amount,
        string Currency,
        string IdempotencyKey,
        int Provider,
        int Method);

    private sealed record CreatePaymentResponse(
        PaymentPayload Payment,
        PaymentActionPayload Action);

    private sealed record PaymentPayload(
        Guid Id,
        Guid OrderId,
        decimal Amount,
        string Currency,
        int Provider,
        int Method,
        int Status,
        string IdempotencyKey,
        DateTime CreatedAtUtc,
        DateTime? AuthorizedAtUtc,
        DateTime? CapturedAtUtc,
        DateTime? RefundedAtUtc,
        string? FailureReason);

    private sealed record PaymentActionPayload(
        string Type,
        string? RedirectUrl,
        string? ClientSecret,
        string? HtmlContent);
}
