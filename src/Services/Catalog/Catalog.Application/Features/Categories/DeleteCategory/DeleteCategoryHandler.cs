using Catalog.Application.Abstractions.Observability;
using Catalog.Application.Abstractions.Persistence;
using Catalog.Application.Common.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Catalog.Application.Features.Categories.DeleteCategory;

public class DeleteCategoryHandler(
    ICatalogDbContext context,
    ICatalogMetrics metrics,
    ILogger<DeleteCategoryHandler> logger) : IRequestHandler<DeleteCategoryCommand>
{
    public async Task Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await context.Categories.FirstOrDefaultAsync(x => x.Id == request.CategoryId, cancellationToken);
        if (category is null)
        {
            throw new NotFoundException($"Category '{request.CategoryId}' was not found.");
        }

        var hasProducts = await context.Products.AnyAsync(x => x.CategoryId == request.CategoryId, cancellationToken);
        if (hasProducts)
        {
            throw new ConflictException("Category cannot be deleted because it has products.");
        }

        var hasChildCategories = await context.Categories.AnyAsync(
            x => x.ParentCategoryId == request.CategoryId,
            cancellationToken);
        if (hasChildCategories)
        {
            throw new ConflictException("Category cannot be deleted because it has child categories.");
        }

        category.MarkAsDeleted();
        await context.SaveChangesAsync(cancellationToken);

        metrics.RecordCategoryDeleted();
        logger.LogInformation(
            "Category marked as deleted for {CategoryId}",
            category.Id);
    }
}
