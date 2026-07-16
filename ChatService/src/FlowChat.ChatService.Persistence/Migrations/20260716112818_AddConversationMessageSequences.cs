using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlowChat.ChatService.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddConversationMessageSequences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConversationMessageSequences",
                columns: table => new
                {
                    ConversationId = table.Column<Guid>(type: "uuid", nullable: false),
                    LastAssignedSequenceNum = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConversationMessageSequences", x => x.ConversationId);
                    table.ForeignKey(
                        name: "FK_ConversationMessageSequences_Conversations_ConversationId",
                        column: x => x.ConversationId,
                        principalTable: "Conversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql(
                """
                INSERT INTO "ConversationMessageSequences" ("ConversationId", "LastAssignedSequenceNum")
                SELECT conversation."Id", COALESCE(MAX(message."SequenceNum"), 0)
                FROM "Conversations" AS conversation
                LEFT JOIN "ChatMessages" AS message ON message."ConversationId" = conversation."Id"
                GROUP BY conversation."Id";
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConversationMessageSequences");
        }
    }
}
