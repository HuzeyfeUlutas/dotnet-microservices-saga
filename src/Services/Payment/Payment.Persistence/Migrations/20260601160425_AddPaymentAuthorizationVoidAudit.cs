using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Payment.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentAuthorizationVoidAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AuthorizationVoidFailedAtUtc",
                table: "payments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AuthorizationVoidedAtUtc",
                table: "payments",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AuthorizationVoidFailedAtUtc",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "AuthorizationVoidedAtUtc",
                table: "payments");
        }
    }
}
