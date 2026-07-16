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
                name: "RealtimeGroupMembershipReadModels",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    GroupType = table.Column<string>(type: "text", nullable: false),
                    ResourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RealtimeGroupMembershipReadModels", x => new { x.UserId, x.GroupType, x.ResourceId });
                });

            migrationBuilder.CreateTable(
                name: "RealtimeGroupMembershipRevisionTrackerReadModels",
                columns: table => new
                {
                    ConversationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Revision = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RealtimeGroupMembershipRevisionTrackerReadModels", x => x.ConversationId);
                });

            migrationBuilder.CreateTable(
                name: "SilverbackStoredOffsets",
                columns: table => new
                {
                    GroupId = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Topic = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Partition = table.Column<int>(type: "integer", nullable: false),
                    Offset = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SilverbackStoredOffsets", x => new { x.GroupId, x.Topic, x.Partition });
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RealtimeGroupMembershipReadModels");

            migrationBuilder.DropTable(
                name: "RealtimeGroupMembershipRevisionTrackerReadModels");

            migrationBuilder.DropTable(
                name: "SilverbackStoredOffsets");
        }
    }
}
