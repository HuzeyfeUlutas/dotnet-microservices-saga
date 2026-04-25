using Catalog.Application.Abstractions.Persistence;
using Catalog.Application.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Application.Features.Brands.GetBrands;

public class GetBrandsHandler(ICatalogDbContext context)
    : IRequestHandler<GetBrandsQuery, IReadOnlyCollection<BrandListItemDto>>
{
    public async Task<IReadOnlyCollection<BrandListItemDto>> Handle(
        GetBrandsQuery request,
        CancellationToken cancellationToken)
    {
        var brands = await context.Brands
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new BrandListItemDto(
                x.Id,
                x.Name,
                x.IsActive))
            .ToListAsync(cancellationToken);

        return brands;
    }
}
