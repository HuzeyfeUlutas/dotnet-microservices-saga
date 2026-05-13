using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueReservationPerInventoryItemOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_inventory_reservations_InventoryItemId_OrderId",
                table: "inventory_reservations");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_reservations_InventoryItemId_OrderId",
                table: "inventory_reservations",
                columns: new[] { "InventoryItemId", "OrderId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_inventory_reservations_InventoryItemId_OrderId",
                table: "inventory_reservations");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_reservations_InventoryItemId_OrderId",
                table: "inventory_reservations",
                columns: new[] { "InventoryItemId", "OrderId" });
        }
    }
}
