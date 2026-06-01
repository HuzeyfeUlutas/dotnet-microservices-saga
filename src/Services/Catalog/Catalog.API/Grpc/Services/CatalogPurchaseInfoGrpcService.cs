using System.Globalization;
using Catalog.Application.Common.Exceptions;
using Catalog.Application.Features.Products.GetProductPurchaseInfo;
using FluentValidation;
using Grpc.Core;
using Marketplace.Grpc.Catalog.V1;
using MediatR;

namespace Catalog.API.Grpc.Services;

public sealed class CatalogPurchaseInfoGrpcService(ISender sender) : CatalogPurchaseInfo.CatalogPurchaseInfoBase
{
    public override async Task<GetPurchaseInfoResponse> GetPurchaseInfo(
        GetPurchaseInfoRequest request,
        ServerCallContext context)
    {
        if (!Guid.TryParse(request.ProductId, out var productId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Product id must be a valid GUID."));
        }

        try
        {
            var purchaseInfo = await sender.Send(
                new GetProductPurchaseInfoQuery(productId, request.Sku),
                context.CancellationToken);

            return new GetPurchaseInfoResponse
            {
                ProductId = purchaseInfo.ProductId.ToString(),
                ProductName = purchaseInfo.ProductName,
                Sku = purchaseInfo.Sku,
                VariantName = purchaseInfo.VariantName,
                UnitPrice = purchaseInfo.UnitPrice.ToString(CultureInfo.InvariantCulture),
                Currency = purchaseInfo.Currency,
                ProductStatus = (int)purchaseInfo.ProductStatus,
                VariantStatus = (int)purchaseInfo.VariantStatus,
                BrandId = purchaseInfo.BrandId.ToString(),
                CategoryId = purchaseInfo.CategoryId.ToString(),
                IsPurchasable = purchaseInfo.IsPurchasable,
                NotPurchasableReason = purchaseInfo.NotPurchasableReason ?? string.Empty
            };
        }
        catch (ValidationException exception)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, exception.Message));
        }
        catch (NotFoundException exception)
        {
            throw new RpcException(new Status(StatusCode.NotFound, exception.Message));
        }
    }
}
