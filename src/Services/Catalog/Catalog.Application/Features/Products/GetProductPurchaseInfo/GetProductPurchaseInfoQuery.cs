using Catalog.Application.DTOs;
using MediatR;

namespace Catalog.Application.Features.Products.GetProductPurchaseInfo;

public sealed record GetProductPurchaseInfoQuery(
    Guid ProductId,
    string Sku) : IRequest<ProductPurchaseInfoDto>;
