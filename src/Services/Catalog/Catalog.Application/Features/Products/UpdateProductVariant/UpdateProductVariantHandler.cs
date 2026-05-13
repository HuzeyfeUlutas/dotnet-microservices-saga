using Catalog.Application.Abstractions.Persistence;
using Catalog.Application.Common.Exceptions;
using Catalog.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Application.Features.Products.UpdateProductVariant;

public class UpdateProductVariantHandler(ICatalogDbContext context)
    : IRequestHandler<UpdateProductVariantCommand>
{
    public async Task Handle(UpdateProductVariantCommand request, CancellationToken cancellationToken)
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
            throw new ConflictException("Variant cannot be updated on an archived product.");
        }

        var variant = product.Variants.SingleOrDefault(x => x.Id == request.VariantId);
        if (variant is null)
        {
            throw new NotFoundException($"Product variant '{request.VariantId}' was not found.");
        }

        var normalizedSku = request.Sku.Trim();
        var skuExists = product.Variants.Any(x =>
            x.Id != request.VariantId &&
            string.Equals(x.Sku, normalizedSku, StringComparison.OrdinalIgnoreCase));
        if (skuExists)
        {
            throw new ConflictException($"Variant SKU '{normalizedSku}' already exists for this product.");
        }

        variant.UpdateDetails(request.Name, request.Sku);
        await context.SaveChangesAsync(cancellationToken);
    }
}
