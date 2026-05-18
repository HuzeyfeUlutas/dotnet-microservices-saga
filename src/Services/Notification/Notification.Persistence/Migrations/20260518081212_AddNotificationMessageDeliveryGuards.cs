using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Notification.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationMessageDeliveryGuards : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_notification_messages_SourceEventId_NotificationType_Recipi~",
                table: "notification_messages");

            migrationBuilder.AddColumn<long>(
                name: "ConcurrencyVersion",
                table: "notification_messages",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "RecipientId",
                table: "notification_messages",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_notification_messages_SourceEventId_NotificationType_Recipi~",
                table: "notification_messages",
                columns: new[] { "SourceEventId", "NotificationType", "RecipientId" },
                unique: true,
                filter: "\"SourceEventId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_notification_messages_SourceEventId_NotificationType_Recipi~",
                table: "notification_messages");

            migrationBuilder.DropColumn(
                name: "ConcurrencyVersion",
                table: "notification_messages");

            migrationBuilder.DropColumn(
                name: "RecipientId",
                table: "notification_messages");

            migrationBuilder.CreateIndex(
                name: "IX_notification_messages_SourceEventId_NotificationType_Recipi~",
                table: "notification_messages",
                columns: new[] { "SourceEventId", "NotificationType", "Recipient" },
                unique: true,
                filter: "\"SourceEventId\" IS NOT NULL");
        }
    }
}
