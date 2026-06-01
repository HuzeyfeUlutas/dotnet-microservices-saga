using System.Globalization;
using Grpc.Core;
using Marketplace.Grpc.Catalog.V1;
using Order.Application.Abstractions.Services;
using Order.Application.Common.Exceptions;
using Order.Infrastructure.Configuration;

namespace Order.Infrastructure.Services;

internal sealed class CatalogPurchaseInfoGrpcClient(
    CatalogPurchaseInfo.CatalogPurchaseInfoClient grpcClient,
    ServiceEndpointOptions options) : ICatalogPurchaseInfoClient
{
    public async Task<CatalogPurchaseInfoDto> GetPurchaseInfoAsync(
        Guid productId,
        string sku,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = await grpcClient.GetPurchaseInfoAsync(
                new GetPurchaseInfoRequest
                {
                    ProductId = productId.ToString(),
                    Sku = sku
                },
                deadline: DateTime.UtcNow.AddSeconds(options.CatalogGrpcTimeoutSeconds),
                cancellationToken: cancellationToken);

            return MapPayload(payload);
        }
        catch (RpcException exception) when (exception.StatusCode == StatusCode.NotFound)
        {
            throw new NotFoundException($"Catalog purchase info for product '{productId}' and SKU '{sku}' was not found.");
        }
        catch (RpcException exception) when (exception.StatusCode == StatusCode.InvalidArgument)
        {
            throw new ConflictException($"Catalog purchase info request was rejected. {exception.Status.Detail}");
        }
        catch (RpcException exception)
        {
            throw new IntegrationException($"Catalog purchase info gRPC request failed with status '{exception.StatusCode}'. {exception.Status.Detail}");
        }
    }

    private static CatalogPurchaseInfoDto MapPayload(GetPurchaseInfoResponse payload)
    {
        if (!Guid.TryParse(payload.ProductId, out var productId) ||
            !Guid.TryParse(payload.BrandId, out var brandId) ||
            !Guid.TryParse(payload.CategoryId, out var categoryId) ||
            !decimal.TryParse(payload.UnitPrice, NumberStyles.Number, CultureInfo.InvariantCulture, out var unitPrice))
        {
            throw new IntegrationException("Catalog purchase info gRPC response contained invalid data.");
        }

        return new CatalogPurchaseInfoDto(
            productId,
            payload.ProductName,
            payload.Sku,
            payload.VariantName,
            unitPrice,
            payload.Currency,
            payload.ProductStatus.ToString(CultureInfo.InvariantCulture),
            payload.VariantStatus.ToString(CultureInfo.InvariantCulture),
            brandId,
            categoryId,
            payload.IsPurchasable,
            string.IsNullOrWhiteSpace(payload.NotPurchasableReason)
                ? null
                : payload.NotPurchasableReason);
    }
}
