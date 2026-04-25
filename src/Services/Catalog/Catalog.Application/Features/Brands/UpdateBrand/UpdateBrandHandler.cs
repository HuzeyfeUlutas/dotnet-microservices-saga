using Catalog.Application.Abstractions.Persistence;
using Catalog.Application.Common.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Application.Features.Brands.UpdateBrand;

public class UpdateBrandHandler(ICatalogDbContext context) : IRequestHandler<UpdateBrandCommand>
{
    public async Task Handle(UpdateBrandCommand request, CancellationToken cancellationToken)
    {
        var brand = await context.Brands.FirstOrDefaultAsync(x => x.Id == request.BrandId, cancellationToken);
        if (brand is null)
        {
            throw new NotFoundException($"Brand '{request.BrandId}' was not found.");
        }

        brand.UpdateDetails(request.Name, request.Description);

        if (request.IsActive)
        {
            brand.Activate();
        }
        else
        {
            brand.Deactivate();
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
