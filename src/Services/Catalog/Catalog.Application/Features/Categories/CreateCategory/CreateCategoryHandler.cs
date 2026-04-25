using Catalog.Application.Abstractions.Persistence;
using Catalog.Application.Common.Exceptions;
using Catalog.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Application.Features.Categories.CreateCategory;

public class CreateCategoryHandler(ICatalogDbContext context) : IRequestHandler<CreateCategoryCommand, Guid>
{
    public async Task<Guid> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
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

        return category.Id;
    }
}
