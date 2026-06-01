using FluentAssertions;
using Grpc.Core;
using Marketplace.Grpc.Catalog.V1;
using Order.Application.Common.Exceptions;
using Order.Infrastructure.Configuration;
using Order.Infrastructure.Services;
using Xunit;

namespace Order.Infrastructure.Tests.Services;

public class CatalogPurchaseInfoGrpcClientTests
{
    [Fact]
    public async Task GetPurchaseInfoAsync_ShouldMapGrpcResponse()
    {
        var productId = Guid.NewGuid();
        var brandId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var grpcClient = new StubCatalogPurchaseInfoGrpcClient
        {
            Response = new GetPurchaseInfoResponse
            {
                ProductId = productId.ToString(),
                ProductName = "Keyboard",
                Sku = "SKU-001",
                VariantName = "Default",
                UnitPrice = "199.90",
                Currency = "TRY",
                ProductStatus = 1,
                VariantStatus = 1,
                BrandId = brandId.ToString(),
                CategoryId = categoryId.ToString(),
                IsPurchasable = true
            }
        };
        var client = CreateClient(grpcClient);

        var result = await client.GetPurchaseInfoAsync(productId, "SKU-001");

        result.ProductId.Should().Be(productId);
        result.UnitPrice.Should().Be(199.90m);
        result.NotPurchasableReason.Should().BeNull();
        grpcClient.Request.Should().NotBeNull();
        grpcClient.Request!.ProductId.Should().Be(productId.ToString());
        grpcClient.Deadline.Should().NotBeNull();
    }

    [Fact]
    public async Task GetPurchaseInfoAsync_ShouldMapNotFoundRpcStatus()
    {
        var grpcClient = new StubCatalogPurchaseInfoGrpcClient
        {
            Exception = new RpcException(new Status(StatusCode.NotFound, "missing"))
        };
        var client = CreateClient(grpcClient);

        var action = async () => await client.GetPurchaseInfoAsync(Guid.NewGuid(), "SKU-001");

        await action.Should().ThrowAsync<NotFoundException>();
    }

    private static CatalogPurchaseInfoGrpcClient CreateClient(
        CatalogPurchaseInfo.CatalogPurchaseInfoClient grpcClient)
    {
        return new CatalogPurchaseInfoGrpcClient(
            grpcClient,
            new ServiceEndpointOptions
            {
                CatalogGrpcTimeoutSeconds = 3
            });
    }

    private sealed class StubCatalogPurchaseInfoGrpcClient : CatalogPurchaseInfo.CatalogPurchaseInfoClient
    {
        public GetPurchaseInfoRequest? Request { get; private set; }
        public GetPurchaseInfoResponse? Response { get; init; }
        public RpcException? Exception { get; init; }
        public DateTime? Deadline { get; private set; }

        public override AsyncUnaryCall<GetPurchaseInfoResponse> GetPurchaseInfoAsync(
            GetPurchaseInfoRequest request,
            Metadata? headers = null,
            DateTime? deadline = null,
            CancellationToken cancellationToken = default)
        {
            Request = request;
            Deadline = deadline;

            return Exception is null
                ? CreateCall(Task.FromResult(Response!))
                : CreateCall(Task.FromException<GetPurchaseInfoResponse>(Exception));
        }

        private static AsyncUnaryCall<GetPurchaseInfoResponse> CreateCall(Task<GetPurchaseInfoResponse> response)
        {
            return new AsyncUnaryCall<GetPurchaseInfoResponse>(
                response,
                Task.FromResult(new Metadata()),
                () => Status.DefaultSuccess,
                () => new Metadata(),
                () => { });
        }
    }
}
