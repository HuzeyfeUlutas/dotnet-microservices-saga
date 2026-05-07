using Inventory.API.Contracts.InventoryItems;
using Inventory.Application.Features.InventoryItems.AdjustStock;
using Inventory.Application.Features.InventoryItems.CreateInventoryItem;
using Inventory.Application.Features.InventoryItems.GetInventoryItemById;
using Inventory.Application.Features.InventoryItems.GetInventoryItemByProductId;
using Inventory.Application.Features.InventoryItems.GetInventoryItems;
using Inventory.Application.Features.InventoryItems.IncreaseStock;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.API.Controllers;

[ApiController]
[Route("api/inventory-items")]
public class InventoryItemsController(ISender sender) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateInventoryItemRequest request,
        CancellationToken cancellationToken)
    {
        var inventoryItemId = await sender.Send(
            new CreateInventoryItemCommand(request.ProductId, request.Sku, request.InitialQuantity),
            cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = inventoryItemId }, new { id = inventoryItemId });
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var inventoryItems = await sender.Send(new GetInventoryItemsQuery(), cancellationToken);
        return Ok(inventoryItems);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var inventoryItem = await sender.Send(new GetInventoryItemByIdQuery(id), cancellationToken);
        return Ok(inventoryItem);
    }

    [HttpGet("by-product/{productId:guid}")]
    public async Task<IActionResult> GetByProductId(Guid productId, CancellationToken cancellationToken)
    {
        var inventoryItem = await sender.Send(new GetInventoryItemByProductIdQuery(productId), cancellationToken);
        return Ok(inventoryItem);
    }

    [HttpPost("{id:guid}/stock-increases")]
    public async Task<IActionResult> IncreaseStock(
        Guid id,
        [FromBody] IncreaseStockRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(
            new IncreaseStockCommand(id, request.Quantity, request.Reason, request.ReferenceId),
            cancellationToken);

        return NoContent();
    }

    [HttpPut("{id:guid}/stock-adjustment")]
    public async Task<IActionResult> AdjustStock(
        Guid id,
        [FromBody] AdjustStockRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(
            new AdjustStockCommand(id, request.NewTotalQuantity, request.Reason, request.ReferenceId),
            cancellationToken);

        return NoContent();
    }
}
