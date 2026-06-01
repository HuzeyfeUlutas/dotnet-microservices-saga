using System.Globalization;
using Grpc.Core;
using Marketplace.Grpc.Payment.V1;
using Order.Application.Abstractions.Services;
using Order.Application.Common.Exceptions;
using Order.Infrastructure.Configuration;

namespace Order.Infrastructure.Services;

internal sealed class PaymentClient(
    PaymentInitiation.PaymentInitiationClient grpcClient,
    ServiceEndpointOptions options) : IPaymentClient
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
        try
        {
            var response = await grpcClient.CreatePaymentAsync(
                new CreatePaymentRequest
                {
                    OrderId = orderId.ToString(),
                    Amount = amount.ToString(CultureInfo.InvariantCulture),
                    Currency = currency,
                    IdempotencyKey = idempotencyKey,
                    Provider = MapProvider(provider),
                    Method = MapMethod(method)
                },
                deadline: DateTime.UtcNow.AddSeconds(options.PaymentGrpcTimeoutSeconds),
                cancellationToken: cancellationToken);

            return MapResponse(response);
        }
        catch (RpcException exception) when (
            exception.StatusCode is StatusCode.InvalidArgument or StatusCode.FailedPrecondition or StatusCode.Aborted)
        {
            throw new ConflictException($"Payment initiation failed. {exception.Status.Detail}");
        }
        catch (RpcException exception)
        {
            throw new IntegrationException($"Payment initiation gRPC request failed with status '{exception.StatusCode}'. {exception.Status.Detail}");
        }
    }

    private static int MapProvider(string provider)
    {
        if (provider.Trim().Equals("Fake", StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }

        throw new ConflictException($"Payment provider '{provider}' is not supported.");
    }

    private static int MapMethod(string method)
    {
        if (method.Trim().Equals("Card", StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }

        throw new ConflictException($"Payment method '{method}' is not supported.");
    }

    private static PaymentInitiationResultDto MapResponse(CreatePaymentResponse response)
    {
        if (!Guid.TryParse(response.PaymentId, out var paymentId))
        {
            throw new IntegrationException("Payment initiation gRPC response contained invalid data.");
        }

        var action = response.Action
                     ?? throw new IntegrationException("Payment initiation gRPC response did not contain an action.");

        return new PaymentInitiationResultDto(
            paymentId,
            MapPaymentStatus(response.Status),
            MapPaymentProvider(response.Provider),
            new PaymentActionDto(
                action.Type,
                NullIfEmpty(action.RedirectUrl),
                NullIfEmpty(action.ClientSecret),
                NullIfEmpty(action.HtmlContent)));
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
            _ => status.ToString(CultureInfo.InvariantCulture)
        };
    }

    private static string MapPaymentProvider(int provider)
    {
        return provider switch
        {
            1 => "Fake",
            _ => provider.ToString(CultureInfo.InvariantCulture)
        };
    }

    private static string? NullIfEmpty(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }
}
