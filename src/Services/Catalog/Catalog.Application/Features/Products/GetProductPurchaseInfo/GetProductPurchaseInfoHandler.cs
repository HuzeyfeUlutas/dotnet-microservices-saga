using Catalog.Application.Abstractions.Persistence;
using Catalog.Application.Common.Exceptions;
using Catalog.Application.DTOs;
using Catalog.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Application.Features.Products.GetProductPurchaseInfo;

public class GetProductPurchaseInfoHandler(ICatalogDbContext context)
    : IRequestHandler<GetProductPurchaseInfoQuery, ProductPurchaseInfoDto>
{
    private const string DefaultCurrency = "TRY";

    public async Task<ProductPurchaseInfoDto> Handle(
        GetProductPurchaseInfoQuery request,
        CancellationToken cancellationToken)
    {
        var normalizedSku = request.Sku.Trim();
        var product = await context.Products
            .AsNoTracking()
            .Include(x => x.Variants)
            .FirstOrDefaultAsync(x => x.Id == request.ProductId, cancellationToken);

        if (product is null)
        {
            throw new NotFoundException($"Product '{request.ProductId}' was not found.");
        }

        var variant = product.Variants.SingleOrDefault(x =>
            string.Equals(x.Sku, normalizedSku, StringComparison.OrdinalIgnoreCase));
        if (variant is null)
        {
            throw new NotFoundException($"Product variant with SKU '{normalizedSku}' was not found.");
        }

        var brand = await context.Brands
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == product.BrandId, cancellationToken);
        if (brand is null)
        {
            throw new NotFoundException($"Brand '{product.BrandId}' was not found.");
        }

        var category = await context.Categories
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == product.CategoryId, cancellationToken);
        if (category is null)
        {
            throw new NotFoundException($"Category '{product.CategoryId}' was not found.");
        }

        var notPurchasableReason = GetNotPurchasableReason(
            product.Status,
            variant.Status,
            brand.IsActive,
            category.IsActive);

        return new ProductPurchaseInfoDto(
            product.Id,
            product.Name,
            variant.Sku,
            variant.Name,
            product.Price,
            DefaultCurrency,
            product.Status,
            variant.Status,
            product.BrandId,
            product.CategoryId,
            notPurchasableReason is null,
            notPurchasableReason);
    }

    private static string? GetNotPurchasableReason(
        ProductStatus productStatus,
        VariantStatus variantStatus,
        bool brandIsActive,
        bool categoryIsActive)
    {
        if (productStatus != ProductStatus.Active)
        {
            return "Product is not active.";
        }

        if (variantStatus != VariantStatus.Active)
        {
            return "Product variant is not active.";
        }

        if (!brandIsActive)
        {
            return "Brand is not active.";
        }

        if (!categoryIsActive)
        {
            return "Category is not active.";
        }

        return null;
    }
}
