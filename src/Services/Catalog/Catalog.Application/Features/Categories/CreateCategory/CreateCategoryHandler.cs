using Catalog.Application.Abstractions.Observability;
using Catalog.Application.Abstractions.Persistence;
using Catalog.Application.Common.Exceptions;
using Catalog.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Catalog.Application.Features.Categories.CreateCategory;

public class CreateCategoryHandler(
    ICatalogDbContext context,
    ICatalogMetrics metrics,
    ILogger<CreateCategoryHandler> logger) : IRequestHandler<CreateCategoryCommand, Guid>
{
    public async Task<Guid> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        var normalizedName = request.Name.Trim();
        var nameExists = await context.Categories
            .AnyAsync(x => x.Name.ToLower() == normalizedName.ToLower(), cancellationToken);
        if (nameExists)
        {
            throw new ConflictException($"Category name '{normalizedName}' already exists.");
        }

        if (request.ParentCategoryId.HasValue)
        {
            var parentExists = await context.Categories.AnyAsync(x => x.Id == request.ParentCategoryId.Value, cancellationToken);
            if (!parentExists)
            {
                throw new NotFoundException($"Category '{request.ParentCategoryId.Value}' was not found.");
            }
        }

        var category = new Category(request.Name, request.Description, request.ParentCategoryId);

        context.Categories.Add(category);
        await context.SaveChangesAsync(cancellationToken);

        metrics.RecordCategoryCreated();
        logger.LogInformation(
            "Category created for {CategoryId} with ParentCategoryId {ParentCategoryId}",
            category.Id,
            category.ParentCategoryId);

        return category.Id;
    }
}
