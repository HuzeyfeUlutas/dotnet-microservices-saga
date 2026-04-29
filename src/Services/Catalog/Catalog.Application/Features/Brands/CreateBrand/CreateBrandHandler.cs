using Catalog.Application.Abstractions.Observability;
using Catalog.Application.Abstractions.Persistence;
using Catalog.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Catalog.Application.Features.Brands.CreateBrand;

public class CreateBrandHandler(
    ICatalogDbContext context,
    ICatalogMetrics metrics,
    ILogger<CreateBrandHandler> logger) : IRequestHandler<CreateBrandCommand, Guid>
{
    public async Task<Guid> Handle(CreateBrandCommand request, CancellationToken cancellationToken)
    {
        var brand = new Brand(request.Name, request.Description);

        context.Brands.Add(brand);
        await context.SaveChangesAsync(cancellationToken);

        metrics.RecordBrandCreated();
        logger.LogInformation(
            "Brand created for {BrandId}",
            brand.Id);

        return brand.Id;
    }
}
