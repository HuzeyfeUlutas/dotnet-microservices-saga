using Catalog.API.Contracts.Brands;
using Catalog.Application.Features.Brands.CreateBrand;
using Catalog.Application.Features.Brands.DeleteBrand;
using Catalog.Application.Features.Brands.GetBrandById;
using Catalog.Application.Features.Brands.GetBrands;
using Catalog.Application.Features.Brands.UpdateBrand;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Catalog.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BrandsController(ISender sender) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateBrandRequest request, CancellationToken cancellationToken)
    {
        var brandId = await sender.Send(new CreateBrandCommand(request.Name, request.Description), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = brandId }, new { id = brandId });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var brand = await sender.Send(new GetBrandByIdQuery(id), cancellationToken);
        return Ok(brand);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var brands = await sender.Send(new GetBrandsQuery(), cancellationToken);
        return Ok(brands);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateBrandRequest request, CancellationToken cancellationToken)
    {
        await sender.Send(new UpdateBrandCommand(id, request.Name, request.Description, request.IsActive), cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteBrandCommand(id), cancellationToken);
        return NoContent();
    }
}
