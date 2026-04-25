using Catalog.Application.Abstractions.Persistence;
using Catalog.Application.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Application.Features.Categories.GetCategories;

public class GetCategoriesHandler(ICatalogDbContext context)
    : IRequestHandler<GetCategoriesQuery, IReadOnlyCollection<CategoryListItemDto>>
{
    public async Task<IReadOnlyCollection<CategoryListItemDto>> Handle(
        GetCategoriesQuery request,
        CancellationToken cancellationToken)
    {
        var categories = await context.Categories
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new CategoryListItemDto(
                x.Id,
                x.Name,
                x.ParentCategoryId,
                x.IsActive))
            .ToListAsync(cancellationToken);

        return categories;
    }
}
