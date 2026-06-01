using Catalog.API.Grpc.Services;
using Catalog.Application.DTOs;
using Catalog.Application.Features.Products.GetProductPurchaseInfo;
using Catalog.Domain.Enums;
using FluentAssertions;
using Grpc.Core;
using Marketplace.Grpc.Catalog.V1;
using MediatR;
using NSubstitute;
using Xunit;

namespace Catalog.API.Tests.Grpc;

public class CatalogPurchaseInfoGrpcServiceTests
{
    [Fact]
    public async Task GetPurchaseInfo_ShouldDelegateToApplicationQueryAndMapResponse()
    {
        var productId = Guid.NewGuid();
        var brandId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<GetProductPurchaseInfoQuery>(), Arg.Any<CancellationToken>())
            .Returns(new ProductPurchaseInfoDto(
                productId,
                "Keyboard",
                "SKU-001",
                "Default",
                199.90m,
                "TRY",
                ProductStatus.Active,
                VariantStatus.Active,
                brandId,
                categoryId,
                true,
                null));
        var service = new CatalogPurchaseInfoGrpcService(sender);

        var response = await service.GetPurchaseInfo(
            new GetPurchaseInfoRequest
            {
                ProductId = productId.ToString(),
                Sku = "SKU-001"
            },
            Substitute.For<ServerCallContext>());

        response.ProductId.Should().Be(productId.ToString());
        response.ProductName.Should().Be("Keyboard");
        response.UnitPrice.Should().Be("199.90");
        response.IsPurchasable.Should().BeTrue();

        await sender.Received(1).Send(
            Arg.Is<GetProductPurchaseInfoQuery>(query =>
                query.ProductId == productId &&
                query.Sku == "SKU-001"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetPurchaseInfo_ShouldRejectInvalidProductId()
    {
        var sender = Substitute.For<ISender>();
        var service = new CatalogPurchaseInfoGrpcService(sender);

        var action = async () => await service.GetPurchaseInfo(
            new GetPurchaseInfoRequest
            {
                ProductId = "invalid-id",
                Sku = "SKU-001"
            },
            Substitute.For<ServerCallContext>());

        var exception = await action.Should().ThrowAsync<RpcException>();
        exception.Which.StatusCode.Should().Be(StatusCode.InvalidArgument);
    }
}
