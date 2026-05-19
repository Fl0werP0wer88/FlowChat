using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlowChat.ChatService.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddChatMessageDeliveredAtUtc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeliveredAtUtc",
                table: "ChatMessages",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeliveredAtUtc",
                table: "ChatMessages");
        }
    }
}
