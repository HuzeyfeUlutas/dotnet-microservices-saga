using Catalog.Application.Abstractions.Persistence;
using Catalog.Application.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Application.Features.Products.GetProducts;

public class GetProductsHandler(ICatalogDbContext context)
    : IRequestHandler<GetProductsQuery, IReadOnlyCollection<ProductListItemDto>>
{
    public async Task<IReadOnlyCollection<ProductListItemDto>> Handle(
        GetProductsQuery request,
        CancellationToken cancellationToken)
    {
        var products = await context.Products
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new ProductListItemDto(
                x.Id,
                x.Name,
                x.Price,
                x.BrandId,
                x.CategoryId,
                x.Status))
            .ToListAsync(cancellationToken);

        return products;
    }
}
