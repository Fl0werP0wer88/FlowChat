using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlowChat.HarnessService.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProjectionTests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Payload = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SourceVersion = table.Column<int>(type: "integer", nullable: false),
                    SourceCreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SourceLastModifiedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SourceDeletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectionTests", x => x.Id);
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
                name: "ProjectionTests");

            migrationBuilder.DropTable(
                name: "SilverbackStoredOffsets");
        }
    }
}
