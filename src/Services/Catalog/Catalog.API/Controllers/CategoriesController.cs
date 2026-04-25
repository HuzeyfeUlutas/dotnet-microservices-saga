using Catalog.API.Contracts.Categories;
using Catalog.Application.Features.Categories.CreateCategory;
using Catalog.Application.Features.Categories.DeleteCategory;
using Catalog.Application.Features.Categories.GetCategories;
using Catalog.Application.Features.Categories.GetCategoryById;
using Catalog.Application.Features.Categories.UpdateCategory;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Catalog.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController(ISender sender) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCategoryRequest request, CancellationToken cancellationToken)
    {
        var categoryId = await sender.Send(
            new CreateCategoryCommand(request.Name, request.Description, request.ParentCategoryId),
            cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = categoryId }, new { id = categoryId });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var category = await sender.Send(new GetCategoryByIdQuery(id), cancellationToken);
        return Ok(category);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var categories = await sender.Send(new GetCategoriesQuery(), cancellationToken);
        return Ok(categories);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCategoryRequest request, CancellationToken cancellationToken)
    {
        await sender.Send(
            new UpdateCategoryCommand(id, request.Name, request.Description, request.ParentCategoryId, request.IsActive),
            cancellationToken);

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteCategoryCommand(id), cancellationToken);
        return NoContent();
    }
}
