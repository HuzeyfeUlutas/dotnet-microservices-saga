using FluentAssertions;
using Inventory.Application.Tests.Support;
using Inventory.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Inventory.Application.Tests.Persistence;

public class InventoryItemConfigurationTests
{
    [Fact]
    public void Inventory_item_should_use_product_id_and_sku_as_unique_boundary()
    {
        using var context = new InventoryTestDbContextFactory().CreateContext();

        var entityType = context.Model.FindEntityType(typeof(InventoryItem));

        entityType.Should().NotBeNull();

        var indexes = entityType!.GetIndexes().ToList();
        var uniqueIndexProperties = indexes
            .Where(index => index.IsUnique)
            .Select(index => index.Properties.Select(property => property.Name).ToArray())
            .ToList();

        uniqueIndexProperties.Should().ContainSingle(properties =>
            properties.SequenceEqual(new[] { nameof(InventoryItem.ProductId), nameof(InventoryItem.Sku) }));

        uniqueIndexProperties.Should().NotContain(properties =>
            properties.SequenceEqual(new[] { nameof(InventoryItem.ProductId) }));

        uniqueIndexProperties.Should().NotContain(properties =>
            properties.SequenceEqual(new[] { nameof(InventoryItem.Sku) }));
    }
}
