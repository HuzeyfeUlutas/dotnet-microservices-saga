using Catalog.Application.Abstractions.Persistence;
using Catalog.Application.Common.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Application.Features.Categories.UpdateCategory;

public class UpdateCategoryHandler(ICatalogDbContext context) : IRequestHandler<UpdateCategoryCommand>
{
    public async Task Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await context.Categories.FirstOrDefaultAsync(x => x.Id == request.CategoryId, cancellationToken);
        if (category is null)
        {
            throw new NotFoundException($"Category '{request.CategoryId}' was not found.");
        }

        if (request.ParentCategoryId.HasValue)
        {
            var parentExists = await context.Categories.AnyAsync(x => x.Id == request.ParentCategoryId.Value, cancellationToken);
            if (!parentExists)
            {
                throw new NotFoundException($"Category '{request.ParentCategoryId.Value}' was not found.");
            }
        }

        category.UpdateDetails(request.Name, request.Description);
        category.ChangeParent(request.ParentCategoryId);

        if (request.IsActive)
        {
            category.Activate();
        }
        else
        {
            category.Deactivate();
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
