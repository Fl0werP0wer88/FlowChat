using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlowChat.ChatService.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RequireChatMessageSequenceNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ChatMessagesV2_ConversationId_SequenceNum",
                table: "ChatMessagesV2");

            migrationBuilder.AlterColumn<long>(
                name: "SequenceNum",
                table: "ChatMessagesV2",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessagesV2_ConversationId_SequenceNum",
                table: "ChatMessagesV2",
                columns: new[] { "ConversationId", "SequenceNum" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ChatMessagesV2_ConversationId_SequenceNum",
                table: "ChatMessagesV2");

            migrationBuilder.AlterColumn<long>(
                name: "SequenceNum",
                table: "ChatMessagesV2",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessagesV2_ConversationId_SequenceNum",
                table: "ChatMessagesV2",
                columns: new[] { "ConversationId", "SequenceNum" },
                unique: true,
                filter: "\"SequenceNum\" IS NOT NULL");
        }
    }
}
