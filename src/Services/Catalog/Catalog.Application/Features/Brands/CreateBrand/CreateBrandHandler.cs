using Catalog.Application.Abstractions.Persistence;
using Catalog.Domain.Entities;
using MediatR;

namespace Catalog.Application.Features.Brands.CreateBrand;

public class CreateBrandHandler(ICatalogDbContext context) : IRequestHandler<CreateBrandCommand, Guid>
{
    public async Task<Guid> Handle(CreateBrandCommand request, CancellationToken cancellationToken)
    {
        var brand = new Brand(request.Name, request.Description);

        context.Brands.Add(brand);
        await context.SaveChangesAsync(cancellationToken);

        return brand.Id;
    }
}
