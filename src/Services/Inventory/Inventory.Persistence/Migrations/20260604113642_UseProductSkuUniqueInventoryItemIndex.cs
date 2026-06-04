using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UseProductSkuUniqueInventoryItemIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_inventory_items_ProductId",
                table: "inventory_items");

            migrationBuilder.DropIndex(
                name: "IX_inventory_items_Sku",
                table: "inventory_items");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_items_ProductId_Sku",
                table: "inventory_items",
                columns: new[] { "ProductId", "Sku" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_inventory_items_ProductId_Sku",
                table: "inventory_items");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_items_ProductId",
                table: "inventory_items",
                column: "ProductId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_inventory_items_Sku",
                table: "inventory_items",
                column: "Sku",
                unique: true);
        }
    }
}
