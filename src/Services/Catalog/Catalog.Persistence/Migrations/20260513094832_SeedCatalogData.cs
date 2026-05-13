using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Catalog.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedCatalogData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "brands",
                columns: new[] { "Id", "CreatedAtUtc", "CreatedBy", "DeletedAtUtc", "DeletedBy", "Description", "IsActive", "Name", "UpdatedAtUtc", "UpdatedBy" },
                values: new object[,]
                {
                    { new Guid("a1f08f2b-9b5f-41a0-bc86-bb1131a6e101"), new DateTime(2026, 5, 13, 0, 0, 0, 0, DateTimeKind.Utc), "catalog-seed", null, null, "Everyday consumer electronics and accessories.", true, "TechGear", null, null },
                    { new Guid("a1f08f2b-9b5f-41a0-bc86-bb1131a6e102"), new DateTime(2026, 5, 13, 0, 0, 0, 0, DateTimeKind.Utc), "catalog-seed", null, null, "Audio products for work and travel.", true, "SoundCore", null, null },
                    { new Guid("a1f08f2b-9b5f-41a0-bc86-bb1131a6e103"), new DateTime(2026, 5, 13, 0, 0, 0, 0, DateTimeKind.Utc), "catalog-seed", null, null, "Inactive supplier used for unavailable product scenarios.", false, "LegacyLine", null, null }
                });

            migrationBuilder.InsertData(
                table: "categories",
                columns: new[] { "Id", "CreatedAtUtc", "CreatedBy", "DeletedAtUtc", "DeletedBy", "Description", "IsActive", "Name", "ParentCategoryId", "UpdatedAtUtc", "UpdatedBy" },
                values: new object[,]
                {
                    { new Guid("b2f08f2b-9b5f-41a0-bc86-bb1131a6e201"), new DateTime(2026, 5, 13, 0, 0, 0, 0, DateTimeKind.Utc), "catalog-seed", null, null, "Electronic devices and peripherals.", true, "Electronics", null, null, null },
                    { new Guid("b2f08f2b-9b5f-41a0-bc86-bb1131a6e204"), new DateTime(2026, 5, 13, 0, 0, 0, 0, DateTimeKind.Utc), "catalog-seed", null, null, "Inactive category used for unavailable product scenarios.", false, "Clearance", null, null, null },
                    { new Guid("b2f08f2b-9b5f-41a0-bc86-bb1131a6e202"), new DateTime(2026, 5, 13, 0, 0, 0, 0, DateTimeKind.Utc), "catalog-seed", null, null, "Smartphones and mobile devices.", true, "Phones", new Guid("b2f08f2b-9b5f-41a0-bc86-bb1131a6e201"), null, null },
                    { new Guid("b2f08f2b-9b5f-41a0-bc86-bb1131a6e203"), new DateTime(2026, 5, 13, 0, 0, 0, 0, DateTimeKind.Utc), "catalog-seed", null, null, "Computer and mobile accessories.", true, "Accessories", new Guid("b2f08f2b-9b5f-41a0-bc86-bb1131a6e201"), null, null }
                });

            migrationBuilder.InsertData(
                table: "products",
                columns: new[] { "Id", "BrandId", "CategoryId", "CreatedAtUtc", "CreatedBy", "DeletedAtUtc", "DeletedBy", "Description", "Name", "Price", "Status", "UpdatedAtUtc", "UpdatedBy" },
                values: new object[] { new Guid("c3f08f2b-9b5f-41a0-bc86-bb1131a6e305"), new Guid("a1f08f2b-9b5f-41a0-bc86-bb1131a6e101"), new Guid("b2f08f2b-9b5f-41a0-bc86-bb1131a6e204"), new DateTime(2026, 5, 13, 0, 0, 0, 0, DateTimeKind.Utc), "catalog-seed", null, null, "Active product under an inactive category.", "Clearance USB-C Cable", 149.90m, 2, null, null });

            migrationBuilder.InsertData(
                table: "product_variants",
                columns: new[] { "Id", "CreatedAtUtc", "CreatedBy", "DeletedAtUtc", "DeletedBy", "Name", "ProductId", "Sku", "Status", "UpdatedAtUtc", "UpdatedBy" },
                values: new object[] { new Guid("d4f08f2b-9b5f-41a0-bc86-bb1131a6e407"), new DateTime(2026, 5, 13, 0, 0, 0, 0, DateTimeKind.Utc), "catalog-seed", null, null, "1m", new Guid("c3f08f2b-9b5f-41a0-bc86-bb1131a6e305"), "CUC-1M", 1, null, null });

            migrationBuilder.InsertData(
                table: "products",
                columns: new[] { "Id", "BrandId", "CategoryId", "CreatedAtUtc", "CreatedBy", "DeletedAtUtc", "DeletedBy", "Description", "Name", "Price", "Status", "UpdatedAtUtc", "UpdatedBy" },
                values: new object[,]
                {
                    { new Guid("c3f08f2b-9b5f-41a0-bc86-bb1131a6e301"), new Guid("a1f08f2b-9b5f-41a0-bc86-bb1131a6e101"), new Guid("b2f08f2b-9b5f-41a0-bc86-bb1131a6e203"), new DateTime(2026, 5, 13, 0, 0, 0, 0, DateTimeKind.Utc), "catalog-seed", null, null, "Compact wireless mouse with USB receiver.", "Wireless Mouse", 799.90m, 2, null, null },
                    { new Guid("c3f08f2b-9b5f-41a0-bc86-bb1131a6e302"), new Guid("a1f08f2b-9b5f-41a0-bc86-bb1131a6e102"), new Guid("b2f08f2b-9b5f-41a0-bc86-bb1131a6e203"), new DateTime(2026, 5, 13, 0, 0, 0, 0, DateTimeKind.Utc), "catalog-seed", null, null, "Over-ear Bluetooth headphones with active noise cancellation.", "Noise Cancelling Headphones", 3499.00m, 2, null, null },
                    { new Guid("c3f08f2b-9b5f-41a0-bc86-bb1131a6e303"), new Guid("a1f08f2b-9b5f-41a0-bc86-bb1131a6e101"), new Guid("b2f08f2b-9b5f-41a0-bc86-bb1131a6e203"), new DateTime(2026, 5, 13, 0, 0, 0, 0, DateTimeKind.Utc), "catalog-seed", null, null, "Archived product used to verify non-purchasable product status.", "Mechanical Keyboard", 2199.00m, 4, null, null },
                    { new Guid("c3f08f2b-9b5f-41a0-bc86-bb1131a6e304"), new Guid("a1f08f2b-9b5f-41a0-bc86-bb1131a6e103"), new Guid("b2f08f2b-9b5f-41a0-bc86-bb1131a6e203"), new DateTime(2026, 5, 13, 0, 0, 0, 0, DateTimeKind.Utc), "catalog-seed", null, null, "Active product under an inactive brand.", "Legacy Fast Charger", 499.00m, 2, null, null }
                });

            migrationBuilder.InsertData(
                table: "product_images",
                columns: new[] { "Id", "AltText", "CreatedAtUtc", "CreatedBy", "DeletedAtUtc", "DeletedBy", "ImageUrl", "IsPrimary", "ProductId", "SortOrder", "UpdatedAtUtc", "UpdatedBy" },
                values: new object[,]
                {
                    { new Guid("e5f08f2b-9b5f-41a0-bc86-bb1131a6e501"), "Black wireless mouse", new DateTime(2026, 5, 13, 0, 0, 0, 0, DateTimeKind.Utc), "catalog-seed", null, null, "https://cdn.marketplace.local/catalog/wireless-mouse-black.jpg", true, new Guid("c3f08f2b-9b5f-41a0-bc86-bb1131a6e301"), 0, null, null },
                    { new Guid("e5f08f2b-9b5f-41a0-bc86-bb1131a6e502"), "Noise cancelling headphones", new DateTime(2026, 5, 13, 0, 0, 0, 0, DateTimeKind.Utc), "catalog-seed", null, null, "https://cdn.marketplace.local/catalog/noise-cancelling-headphones.jpg", true, new Guid("c3f08f2b-9b5f-41a0-bc86-bb1131a6e302"), 0, null, null }
                });

            migrationBuilder.InsertData(
                table: "product_variants",
                columns: new[] { "Id", "CreatedAtUtc", "CreatedBy", "DeletedAtUtc", "DeletedBy", "Name", "ProductId", "Sku", "Status", "UpdatedAtUtc", "UpdatedBy" },
                values: new object[,]
                {
                    { new Guid("d4f08f2b-9b5f-41a0-bc86-bb1131a6e401"), new DateTime(2026, 5, 13, 0, 0, 0, 0, DateTimeKind.Utc), "catalog-seed", null, null, "Black", new Guid("c3f08f2b-9b5f-41a0-bc86-bb1131a6e301"), "WM-BLK", 1, null, null },
                    { new Guid("d4f08f2b-9b5f-41a0-bc86-bb1131a6e402"), new DateTime(2026, 5, 13, 0, 0, 0, 0, DateTimeKind.Utc), "catalog-seed", null, null, "White", new Guid("c3f08f2b-9b5f-41a0-bc86-bb1131a6e301"), "WM-WHT", 1, null, null },
                    { new Guid("d4f08f2b-9b5f-41a0-bc86-bb1131a6e403"), new DateTime(2026, 5, 13, 0, 0, 0, 0, DateTimeKind.Utc), "catalog-seed", null, null, "Black", new Guid("c3f08f2b-9b5f-41a0-bc86-bb1131a6e302"), "NCH-BLK", 1, null, null },
                    { new Guid("d4f08f2b-9b5f-41a0-bc86-bb1131a6e404"), new DateTime(2026, 5, 13, 0, 0, 0, 0, DateTimeKind.Utc), "catalog-seed", null, null, "Silver", new Guid("c3f08f2b-9b5f-41a0-bc86-bb1131a6e302"), "NCH-SLV", 2, null, null },
                    { new Guid("d4f08f2b-9b5f-41a0-bc86-bb1131a6e405"), new DateTime(2026, 5, 13, 0, 0, 0, 0, DateTimeKind.Utc), "catalog-seed", null, null, "TR Layout", new Guid("c3f08f2b-9b5f-41a0-bc86-bb1131a6e303"), "MK-TR", 1, null, null },
                    { new Guid("d4f08f2b-9b5f-41a0-bc86-bb1131a6e406"), new DateTime(2026, 5, 13, 0, 0, 0, 0, DateTimeKind.Utc), "catalog-seed", null, null, "65W", new Guid("c3f08f2b-9b5f-41a0-bc86-bb1131a6e304"), "LFC-65W", 1, null, null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "Id",
                keyValue: new Guid("b2f08f2b-9b5f-41a0-bc86-bb1131a6e202"));

            migrationBuilder.DeleteData(
                table: "product_images",
                keyColumn: "Id",
                keyValue: new Guid("e5f08f2b-9b5f-41a0-bc86-bb1131a6e501"));

            migrationBuilder.DeleteData(
                table: "product_images",
                keyColumn: "Id",
                keyValue: new Guid("e5f08f2b-9b5f-41a0-bc86-bb1131a6e502"));

            migrationBuilder.DeleteData(
                table: "product_variants",
                keyColumn: "Id",
                keyValue: new Guid("d4f08f2b-9b5f-41a0-bc86-bb1131a6e401"));

            migrationBuilder.DeleteData(
                table: "product_variants",
                keyColumn: "Id",
                keyValue: new Guid("d4f08f2b-9b5f-41a0-bc86-bb1131a6e402"));

            migrationBuilder.DeleteData(
                table: "product_variants",
                keyColumn: "Id",
                keyValue: new Guid("d4f08f2b-9b5f-41a0-bc86-bb1131a6e403"));

            migrationBuilder.DeleteData(
                table: "product_variants",
                keyColumn: "Id",
                keyValue: new Guid("d4f08f2b-9b5f-41a0-bc86-bb1131a6e404"));

            migrationBuilder.DeleteData(
                table: "product_variants",
                keyColumn: "Id",
                keyValue: new Guid("d4f08f2b-9b5f-41a0-bc86-bb1131a6e405"));

            migrationBuilder.DeleteData(
                table: "product_variants",
                keyColumn: "Id",
                keyValue: new Guid("d4f08f2b-9b5f-41a0-bc86-bb1131a6e406"));

            migrationBuilder.DeleteData(
                table: "product_variants",
                keyColumn: "Id",
                keyValue: new Guid("d4f08f2b-9b5f-41a0-bc86-bb1131a6e407"));

            migrationBuilder.DeleteData(
                table: "products",
                keyColumn: "Id",
                keyValue: new Guid("c3f08f2b-9b5f-41a0-bc86-bb1131a6e301"));

            migrationBuilder.DeleteData(
                table: "products",
                keyColumn: "Id",
                keyValue: new Guid("c3f08f2b-9b5f-41a0-bc86-bb1131a6e302"));

            migrationBuilder.DeleteData(
                table: "products",
                keyColumn: "Id",
                keyValue: new Guid("c3f08f2b-9b5f-41a0-bc86-bb1131a6e303"));

            migrationBuilder.DeleteData(
                table: "products",
                keyColumn: "Id",
                keyValue: new Guid("c3f08f2b-9b5f-41a0-bc86-bb1131a6e304"));

            migrationBuilder.DeleteData(
                table: "products",
                keyColumn: "Id",
                keyValue: new Guid("c3f08f2b-9b5f-41a0-bc86-bb1131a6e305"));

            migrationBuilder.DeleteData(
                table: "brands",
                keyColumn: "Id",
                keyValue: new Guid("a1f08f2b-9b5f-41a0-bc86-bb1131a6e101"));

            migrationBuilder.DeleteData(
                table: "brands",
                keyColumn: "Id",
                keyValue: new Guid("a1f08f2b-9b5f-41a0-bc86-bb1131a6e102"));

            migrationBuilder.DeleteData(
                table: "brands",
                keyColumn: "Id",
                keyValue: new Guid("a1f08f2b-9b5f-41a0-bc86-bb1131a6e103"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "Id",
                keyValue: new Guid("b2f08f2b-9b5f-41a0-bc86-bb1131a6e203"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "Id",
                keyValue: new Guid("b2f08f2b-9b5f-41a0-bc86-bb1131a6e204"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "Id",
                keyValue: new Guid("b2f08f2b-9b5f-41a0-bc86-bb1131a6e201"));
        }
    }
}
