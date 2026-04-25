using Catalog.Application.Abstractions.Persistence;
using Catalog.Application.Common.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Application.Features.Products.DeleteProduct;

public class DeleteProductHandler(ICatalogDbContext context) : IRequestHandler<DeleteProductCommand>
{
    public async Task Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        var product = await context.Products.FirstOrDefaultAsync(x => x.Id == request.ProductId, cancellationToken);
        if (product is null)
        {
            throw new NotFoundException($"Product '{request.ProductId}' was not found.");
        }

        product.MarkAsDeleted();
        await context.SaveChangesAsync(cancellationToken);
    }
}
