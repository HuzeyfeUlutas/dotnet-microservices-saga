using Catalog.Application.Abstractions.Persistence;
using Catalog.Application.Common.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Application.Features.Products.DeactivateProductVariant;

public class DeactivateProductVariantHandler(ICatalogDbContext context)
    : IRequestHandler<DeactivateProductVariantCommand>
{
    public async Task Handle(DeactivateProductVariantCommand request, CancellationToken cancellationToken)
    {
        var product = await context.Products
            .Include(x => x.Variants)
            .FirstOrDefaultAsync(x => x.Id == request.ProductId, cancellationToken);

        if (product is null)
        {
            throw new NotFoundException($"Product '{request.ProductId}' was not found.");
        }

        var variant = product.Variants.SingleOrDefault(x => x.Id == request.VariantId);
        if (variant is null)
        {
            throw new NotFoundException($"Product variant '{request.VariantId}' was not found.");
        }

        variant.Deactivate();
        await context.SaveChangesAsync(cancellationToken);
    }
}
