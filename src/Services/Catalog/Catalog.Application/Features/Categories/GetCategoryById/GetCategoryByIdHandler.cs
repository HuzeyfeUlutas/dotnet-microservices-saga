using Catalog.Application.Abstractions.Persistence;
using Catalog.Application.Common.Exceptions;
using Catalog.Application.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Application.Features.Categories.GetCategoryById;

public class GetCategoryByIdHandler(ICatalogDbContext context) : IRequestHandler<GetCategoryByIdQuery, CategoryDto>
{
    public async Task<CategoryDto> Handle(GetCategoryByIdQuery request, CancellationToken cancellationToken)
    {
        var category = await context.Categories
            .AsNoTracking()
            .Where(x => x.Id == request.CategoryId)
            .Select(x => new CategoryDto(
                x.Id,
                x.Name,
                x.Description,
                x.ParentCategoryId,
                x.IsActive))
            .FirstOrDefaultAsync(cancellationToken);

        return category ?? throw new NotFoundException($"Category '{request.CategoryId}' was not found.");
    }
}
