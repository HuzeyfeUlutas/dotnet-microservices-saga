using Catalog.API.Contracts.Products;
using Catalog.Application.Features.Products.CreateProduct;
using Catalog.Application.Features.Products.DeleteProduct;
using Catalog.Application.Features.Products.GetProductById;
using Catalog.Application.Features.Products.GetProducts;
using Catalog.Application.Features.Products.UpdateProduct;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Catalog.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController(ISender sender) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProductRequest request, CancellationToken cancellationToken)
    {
        var productId = await sender.Send(
            new CreateProductCommand(request.Name, request.Description, request.Price, request.BrandId, request.CategoryId),
            cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = productId }, new { id = productId });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var product = await sender.Send(new GetProductByIdQuery(id), cancellationToken);
        return Ok(product);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var products = await sender.Send(new GetProductsQuery(), cancellationToken);
        return Ok(products);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProductRequest request, CancellationToken cancellationToken)
    {
        await sender.Send(
            new UpdateProductCommand(
                id,
                request.Name,
                request.Description,
                request.Price,
                request.BrandId,
                request.CategoryId,
                request.Status),
            cancellationToken);

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteProductCommand(id), cancellationToken);
        return NoContent();
    }
}
