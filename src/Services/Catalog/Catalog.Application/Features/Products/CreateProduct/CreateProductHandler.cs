using Catalog.Application.Abstractions.Persistence;
using Catalog.Application.Common.Exceptions;
using Catalog.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Application.Features.Products.CreateProduct;

// Product olusturma use-case'ini tek transaction icinde yurutur.
public class CreateProductHandler(ICatalogDbContext context) : IRequestHandler<CreateProductCommand, Guid>
{
    public async Task<Guid> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
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

        var product = new Product(
            request.Name,
            request.Description,
            request.Price,
            request.BrandId,
            request.CategoryId);

        context.Products.Add(product);
        await context.SaveChangesAsync(cancellationToken);

        return product.Id;
    }
}
