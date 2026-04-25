using Catalog.Application.Abstractions.Persistence;
using Catalog.Application.Common.Exceptions;
using Catalog.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Application.Features.Products.UpdateProduct;

public class UpdateProductHandler(ICatalogDbContext context) : IRequestHandler<UpdateProductCommand>
{
    public async Task Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var product = await context.Products.FirstOrDefaultAsync(x => x.Id == request.ProductId, cancellationToken);
        if (product is null)
        {
            throw new NotFoundException($"Product '{request.ProductId}' was not found.");
        }

        var brandExists = await context.Brands.AnyAsync(x => x.Id == request.BrandId, cancellationToken);
        if (!brandExists)
        {
            throw new NotFoundException($"Brand '{request.BrandId}' was not found.");
        }

        var categoryExists = await context.Categories.AnyAsync(x => x.Id == request.CategoryId, cancellationToken);
        if (!categoryExists)
        {
            throw new NotFoundException($"Category '{request.CategoryId}' was not found.");
        }

        product.UpdateDetails(request.Name, request.Description);
        product.ChangePrice(request.Price);
        product.ChangeBrand(request.BrandId);
        product.ChangeCategory(request.CategoryId);

        switch (request.Status)
        {
            case ProductStatus.Active:
                product.Activate();
                break;
            case ProductStatus.Inactive:
                product.Deactivate();
                break;
            case ProductStatus.Archived:
                product.Archive();
                break;
            default:
                break;
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
