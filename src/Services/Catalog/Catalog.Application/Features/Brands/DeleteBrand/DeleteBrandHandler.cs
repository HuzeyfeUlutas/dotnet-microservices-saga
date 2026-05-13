using Catalog.Application.Abstractions.Observability;
using Catalog.Application.Abstractions.Persistence;
using Catalog.Application.Common.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Catalog.Application.Features.Brands.DeleteBrand;

public class DeleteBrandHandler(
    ICatalogDbContext context,
    ICatalogMetrics metrics,
    ILogger<DeleteBrandHandler> logger) : IRequestHandler<DeleteBrandCommand>
{
    public async Task Handle(DeleteBrandCommand request, CancellationToken cancellationToken)
    {
        var brand = await context.Brands.FirstOrDefaultAsync(x => x.Id == request.BrandId, cancellationToken);
        if (brand is null)
        {
            throw new NotFoundException($"Brand '{request.BrandId}' was not found.");
        }

        var hasProducts = await context.Products.AnyAsync(x => x.BrandId == request.BrandId, cancellationToken);
        if (hasProducts)
        {
            throw new ConflictException("Brand cannot be deleted because it has products.");
        }

        brand.MarkAsDeleted();
        await context.SaveChangesAsync(cancellationToken);

        metrics.RecordBrandDeleted();
        logger.LogInformation(
            "Brand marked as deleted for {BrandId}",
            brand.Id);
    }
}
