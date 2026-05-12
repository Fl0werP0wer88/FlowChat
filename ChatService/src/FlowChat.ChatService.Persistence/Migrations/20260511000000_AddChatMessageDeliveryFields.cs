using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlowChat.ChatService.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddChatMessageDeliveryFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DeliveryStatus",
                table: "ChatMessages",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<long>(
                name: "SequenceNum",
                table: "ChatMessages",
                type: "bigint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeliveryStatus",
                table: "ChatMessages");

            migrationBuilder.DropColumn(
                name: "SequenceNum",
                table: "ChatMessages");
        }
    }
}
