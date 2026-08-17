using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlowChat.ChatService.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class OptimizeDuetContactLookup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_ConversationParticipantsV2_UserId_ConversationId",
                table: "ConversationParticipantsV2",
                columns: new[] { "UserId", "ConversationId" },
                filter: "\"DeletedAt\" IS NULL AND \"IsHidden\" = FALSE AND \"DuetPartnerUserId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ConversationParticipantsV2_UserId_ConversationId",
                table: "ConversationParticipantsV2");
        }
    }
}
