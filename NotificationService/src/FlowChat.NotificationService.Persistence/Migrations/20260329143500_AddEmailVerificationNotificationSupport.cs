using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlowChat.NotificationService.Persistence.Migrations
{
    public partial class AddEmailVerificationNotificationSupport : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Notifications_UserId_Type",
                table: "Notifications");

            migrationBuilder.CreateIndex(
                name: "uq_notification_source_message_key",
                table: "Notifications",
                column: "SourceMessageKey",
                unique: true,
                filter: "\"SourceMessageKey\" IS NOT NULL");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "uq_notification_source_message_key",
                table: "Notifications");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId_Type",
                table: "Notifications",
                columns: new[] { "UserId", "Type" },
                unique: true);
        }
    }
}
