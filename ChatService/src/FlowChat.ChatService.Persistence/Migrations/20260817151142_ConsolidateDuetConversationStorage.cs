using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlowChat.ChatService.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ConsolidateDuetConversationStorage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DuetFirstUserId",
                table: "ConversationsV2",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DuetSecondUserId",
                table: "ConversationsV2",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE "ConversationsV2" AS conversation
                SET "DuetFirstUserId" = duet."FirstUserId",
                    "DuetSecondUserId" = duet."SecondUserId"
                FROM "DuetConversationsV2" AS duet
                WHERE conversation."Id" = duet."ConversationId";
                """);

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "ConversationsV2");

            migrationBuilder.DropTable(
                name: "DuetConversationsV2");

            migrationBuilder.CreateIndex(
                name: "UX_ConversationsV2_DuetParticipantPair",
                table: "ConversationsV2",
                columns: new[] { "DuetFirstUserId", "DuetSecondUserId" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL AND \"ConversationType\" = 1");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ConversationsV2_DuetParticipantShape",
                table: "ConversationsV2",
                sql: "(\"ConversationType\" = 1 AND \"DuetFirstUserId\" IS NOT NULL AND \"DuetSecondUserId\" IS NOT NULL) OR (\"ConversationType\" = 2 AND \"DuetFirstUserId\" IS NULL AND \"DuetSecondUserId\" IS NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ConversationsV2_NormalizedDuetParticipants",
                table: "ConversationsV2",
                sql: "\"DuetFirstUserId\" IS NULL OR \"DuetFirstUserId\" < \"DuetSecondUserId\"");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_ConversationsV2_DuetParticipantPair",
                table: "ConversationsV2");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ConversationsV2_DuetParticipantShape",
                table: "ConversationsV2");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ConversationsV2_NormalizedDuetParticipants",
                table: "ConversationsV2");

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "ConversationsV2",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "DuetConversationsV2",
                columns: table => new
                {
                    ConversationId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    FirstUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SecondUserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DuetConversationsV2", x => x.ConversationId);
                    table.CheckConstraint("CK_DuetConversationsV2_NormalizedUsers", "\"FirstUserId\" < \"SecondUserId\"");
                    table.ForeignKey(
                        name: "FK_DuetConversationsV2_ConversationsV2_ConversationId",
                        column: x => x.ConversationId,
                        principalTable: "ConversationsV2",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DuetConversationsV2_FirstUserId_SecondUserId",
                table: "DuetConversationsV2",
                columns: new[] { "FirstUserId", "SecondUserId" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.Sql(
                """
                INSERT INTO "DuetConversationsV2" (
                    "ConversationId",
                    "FirstUserId",
                    "SecondUserId",
                    "DeletedAt")
                SELECT
                    "Id",
                    "DuetFirstUserId",
                    "DuetSecondUserId",
                    "DeletedAt"
                FROM "ConversationsV2"
                WHERE "ConversationType" = 1;
                """);

            migrationBuilder.DropColumn(
                name: "DuetFirstUserId",
                table: "ConversationsV2");

            migrationBuilder.DropColumn(
                name: "DuetSecondUserId",
                table: "ConversationsV2");
        }
    }
}
