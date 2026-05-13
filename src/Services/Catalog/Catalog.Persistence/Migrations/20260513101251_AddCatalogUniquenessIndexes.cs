using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Catalog.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCatalogUniquenessIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE UNIQUE INDEX "IX_brands_Name_CI"
                ON brands (lower("Name"))
                WHERE "IsDeleted" = false;
                """);

            migrationBuilder.Sql("""
                CREATE UNIQUE INDEX "IX_categories_Name_CI"
                ON categories (lower("Name"))
                WHERE "IsDeleted" = false;
                """);

            migrationBuilder.Sql("""
                CREATE UNIQUE INDEX "IX_product_variants_ProductId_Sku_CI"
                ON product_variants ("ProductId", lower("Sku"))
                WHERE "IsDeleted" = false;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""DROP INDEX IF EXISTS "IX_product_variants_ProductId_Sku_CI";""");

            migrationBuilder.Sql("""DROP INDEX IF EXISTS "IX_categories_Name_CI";""");

            migrationBuilder.Sql("""DROP INDEX IF EXISTS "IX_brands_Name_CI";""");
        }
    }
}
