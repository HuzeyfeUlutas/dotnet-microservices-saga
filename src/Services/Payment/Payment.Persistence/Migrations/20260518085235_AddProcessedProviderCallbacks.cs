using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Payment.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProcessedProviderCallbacks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "processed_provider_callbacks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<int>(type: "integer", nullable: false),
                    ProviderEventId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ProcessedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_processed_provider_callbacks", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_processed_provider_callbacks_PaymentId",
                table: "processed_provider_callbacks",
                column: "PaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_processed_provider_callbacks_Provider_ProviderEventId",
                table: "processed_provider_callbacks",
                columns: new[] { "Provider", "ProviderEventId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "processed_provider_callbacks");
        }
    }
}
