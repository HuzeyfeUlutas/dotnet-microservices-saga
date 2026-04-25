using Catalog.Application.Abstractions.Persistence;
using Catalog.Application.Common.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Application.Features.Categories.DeleteCategory;

public class DeleteCategoryHandler(ICatalogDbContext context) : IRequestHandler<DeleteCategoryCommand>
{
    public async Task Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await context.Categories.FirstOrDefaultAsync(x => x.Id == request.CategoryId, cancellationToken);
        if (category is null)
        {
            throw new NotFoundException($"Category '{request.CategoryId}' was not found.");
        }

        category.MarkAsDeleted();
        await context.SaveChangesAsync(cancellationToken);
    }
}
