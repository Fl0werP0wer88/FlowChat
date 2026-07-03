using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlowChat.ChatService.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddParticipantAndConversationIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ParticipantUsers_ConversationId_UserId",
                table: "ParticipantUsers");

            migrationBuilder.CreateIndex(
                name: "IX_ParticipantUsers_ConversationId_UserId",
                table: "ParticipantUsers",
                columns: new[] { "ConversationId", "UserId" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ParticipantUsers_UserId_ConversationId",
                table: "ParticipantUsers",
                columns: new[] { "UserId", "ConversationId" },
                filter: "\"DeletedAt\" IS NULL")
                .Annotation("Npgsql:IndexInclude", new[] { "LastReadMessageSequenceNum" });

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_Type",
                table: "Conversations",
                column: "Type",
                filter: "\"DeletedAt\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ParticipantUsers_ConversationId_UserId",
                table: "ParticipantUsers");

            migrationBuilder.DropIndex(
                name: "IX_ParticipantUsers_UserId_ConversationId",
                table: "ParticipantUsers");

            migrationBuilder.DropIndex(
                name: "IX_Conversations_Type",
                table: "Conversations");

            migrationBuilder.CreateIndex(
                name: "IX_ParticipantUsers_ConversationId_UserId",
                table: "ParticipantUsers",
                columns: new[] { "ConversationId", "UserId" },
                unique: true);
        }
    }
}
