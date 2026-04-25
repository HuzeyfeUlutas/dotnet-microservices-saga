using Catalog.Application.Abstractions.Persistence;
using Catalog.Application.Common.Exceptions;
using Catalog.Application.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Application.Features.Products.GetProductById;

public class GetProductByIdHandler(ICatalogDbContext context) : IRequestHandler<GetProductByIdQuery, ProductDto>
{
    public async Task<ProductDto> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        var product = await context.Products
            .AsNoTracking()
            .Where(x => x.Id == request.ProductId)
            .Select(x => new ProductDto(
                x.Id,
                x.Name,
                x.Description,
                x.Price,
                x.BrandId,
                x.CategoryId,
                x.Status,
                x.Variants
                    .Select(v => new ProductVariantDto(v.Id, v.Name, v.Sku, v.Status))
                    .ToList(),
                x.Images
                    .Select(i => new ProductImageDto(i.Id, i.ImageUrl, i.AltText, i.SortOrder, i.IsPrimary))
                    .ToList()))
            .FirstOrDefaultAsync(cancellationToken);

        return product ?? throw new NotFoundException($"Product '{request.ProductId}' was not found.");
    }
}
