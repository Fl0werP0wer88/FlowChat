using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace FlowChat.NotificationService.Persistence.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260329170000_RemoveLegacyNotificationUserTypeIndex")]
    public partial class RemoveLegacyNotificationUserTypeIndex : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP INDEX IF EXISTS "IX_Notifications_UserId_Type";
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE UNIQUE INDEX IF NOT EXISTS "IX_Notifications_UserId_Type"
                ON "Notifications" ("UserId", "Type");
                """);
        }
    }
}
