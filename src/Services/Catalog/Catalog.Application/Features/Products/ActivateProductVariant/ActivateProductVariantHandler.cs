using Catalog.Application.Abstractions.Persistence;
using Catalog.Application.Common.Exceptions;
using Catalog.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Application.Features.Products.ActivateProductVariant;

public class ActivateProductVariantHandler(ICatalogDbContext context)
    : IRequestHandler<ActivateProductVariantCommand>
{
    public async Task Handle(ActivateProductVariantCommand request, CancellationToken cancellationToken)
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
            throw new ConflictException("Variant cannot be activated on an archived product.");
        }

        var variant = product.Variants.SingleOrDefault(x => x.Id == request.VariantId);
        if (variant is null)
        {
            throw new NotFoundException($"Product variant '{request.VariantId}' was not found.");
        }

        variant.Activate();
        await context.SaveChangesAsync(cancellationToken);
    }
}
