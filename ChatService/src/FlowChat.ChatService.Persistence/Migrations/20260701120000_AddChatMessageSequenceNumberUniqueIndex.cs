using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlowChat.ChatService.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddChatMessageSequenceNumberUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_ConversationId_SequenceNum",
                table: "ChatMessages",
                columns: new[] { "ConversationId", "SequenceNum" },
                unique: true,
                filter: "\"SequenceNum\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ChatMessages_ConversationId_SequenceNum",
                table: "ChatMessages");
        }
    }
}
