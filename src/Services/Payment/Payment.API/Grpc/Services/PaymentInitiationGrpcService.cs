using System.Globalization;
using FluentValidation;
using Grpc.Core;
using Marketplace.Grpc.Payment.V1;
using MediatR;
using Payment.Application.Common.Exceptions;
using Payment.Application.DTOs;
using Payment.Application.Features.Payments.CreatePayment;
using Payment.Domain.Enums;
using Payment.Domain.Exceptions;

namespace Payment.API.Grpc.Services;

public sealed class PaymentInitiationGrpcService(ISender sender) : PaymentInitiation.PaymentInitiationBase
{
    public override async Task<CreatePaymentResponse> CreatePayment(
        CreatePaymentRequest request,
        ServerCallContext context)
    {
        if (!Guid.TryParse(request.OrderId, out var orderId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Order id must be a valid GUID."));
        }

        if (!decimal.TryParse(request.Amount, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Amount must be a valid decimal value."));
        }

        if (!Enum.IsDefined(typeof(PaymentProviderType), request.Provider))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Payment provider is not supported."));
        }

        if (!Enum.IsDefined(typeof(PaymentMethodType), request.Method))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Payment method is not supported."));
        }

        try
        {
            var result = await sender.Send(
                new CreatePaymentCommand(
                    orderId,
                    amount,
                    request.Currency,
                    request.IdempotencyKey,
                    (PaymentProviderType)request.Provider,
                    (PaymentMethodType)request.Method),
                context.CancellationToken);

            return MapResponse(result);
        }
        catch (ValidationException exception)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, exception.Message));
        }
        catch (DomainException exception)
        {
            throw new RpcException(new Status(StatusCode.FailedPrecondition, exception.Message));
        }
        catch (ConflictException exception)
        {
            throw new RpcException(new Status(StatusCode.Aborted, exception.Message));
        }
    }

    private static CreatePaymentResponse MapResponse(CreatePaymentResultDto result)
    {
        var action = new Marketplace.Grpc.Payment.V1.PaymentAction
        {
            Type = result.Action.Type
        };

        if (!string.IsNullOrWhiteSpace(result.Action.RedirectUrl))
        {
            action.RedirectUrl = result.Action.RedirectUrl;
        }

        if (!string.IsNullOrWhiteSpace(result.Action.ClientSecret))
        {
            action.ClientSecret = result.Action.ClientSecret;
        }

        if (!string.IsNullOrWhiteSpace(result.Action.HtmlContent))
        {
            action.HtmlContent = result.Action.HtmlContent;
        }

        return new CreatePaymentResponse
        {
            PaymentId = result.Payment.Id.ToString(),
            Status = (int)result.Payment.Status,
            Provider = (int)result.Payment.Provider,
            Action = action
        };
    }
}
