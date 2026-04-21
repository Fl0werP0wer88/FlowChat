using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlowChat.ChatService.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDuetConversationForeignKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_DuetConversations_ConversationId",
                table: "DuetConversations",
                column: "ConversationId");

            migrationBuilder.AddForeignKey(
                name: "FK_DuetConversations_Conversations_ConversationId",
                table: "DuetConversations",
                column: "ConversationId",
                principalTable: "Conversations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DuetConversations_Conversations_ConversationId",
                table: "DuetConversations");

            migrationBuilder.DropIndex(
                name: "IX_DuetConversations_ConversationId",
                table: "DuetConversations");
        }
    }
}
