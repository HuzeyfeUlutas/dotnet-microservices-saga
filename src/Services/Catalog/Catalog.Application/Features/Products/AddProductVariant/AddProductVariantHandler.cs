using Catalog.Application.Abstractions.Persistence;
using Catalog.Application.Common.Exceptions;
using Catalog.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Application.Features.Products.AddProductVariant;

public class AddProductVariantHandler(ICatalogDbContext context)
    : IRequestHandler<AddProductVariantCommand, Guid>
{
    public async Task<Guid> Handle(AddProductVariantCommand request, CancellationToken cancellationToken)
    {
        var product = await context.Products
            .Include(x => x.Variants)
            .FirstOrDefaultAsync(x => x.Id == request.ProductId, cancellationToken);

        if (product is null)
        {
            throw new NotFoundException($"Product '{request.ProductId}' was not found.");
        }

        if (product.Status == ProductStatus.Archived)
        {
            throw new ConflictException("Variant cannot be added to an archived product.");
        }

        var normalizedSku = request.Sku.Trim();
        var skuExists = product.Variants.Any(x =>
            string.Equals(x.Sku, normalizedSku, StringComparison.OrdinalIgnoreCase));
        if (skuExists)
        {
            throw new ConflictException($"Variant SKU '{normalizedSku}' already exists for this product.");
        }

        var variant = product.AddVariant(request.Name, request.Sku);
        context.ProductVariants.Add(variant);
        await context.SaveChangesAsync(cancellationToken);

        return variant.Id;
    }
}
