using Catalog.Application.Abstractions.Persistence;
using Catalog.Application.Common.Exceptions;
using Catalog.Application.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Application.Features.Brands.GetBrandById;

public class GetBrandByIdHandler(ICatalogDbContext context) : IRequestHandler<GetBrandByIdQuery, BrandDto>
{
    public async Task<BrandDto> Handle(GetBrandByIdQuery request, CancellationToken cancellationToken)
    {
        var brand = await context.Brands
            .AsNoTracking()
            .Where(x => x.Id == request.BrandId)
            .Select(x => new BrandDto(
                x.Id,
                x.Name,
                x.Description,
                x.IsActive))
            .FirstOrDefaultAsync(cancellationToken);

        return brand ?? throw new NotFoundException($"Brand '{request.BrandId}' was not found.");
    }
}
