using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlowChat.RealtimeService.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RealtimeGroupMemberships",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    GroupType = table.Column<string>(type: "text", nullable: false),
                    ResourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RealtimeGroupMemberships", x => new { x.UserId, x.GroupType, x.ResourceId });
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RealtimeGroupMemberships");
        }
    }
}
