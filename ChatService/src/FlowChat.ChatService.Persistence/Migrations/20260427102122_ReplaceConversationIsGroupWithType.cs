using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlowChat.ChatService.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceConversationIsGroupWithType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "Conversations",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "Conversations"
                SET "Type" = CASE WHEN "IsGroup" THEN 2 ELSE 1 END
                """);

            migrationBuilder.AlterColumn<int>(
                name: "Type",
                table: "Conversations",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "IsGroup",
                table: "Conversations");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsGroup",
                table: "Conversations",
                type: "boolean",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "Conversations"
                SET "IsGroup" = CASE WHEN "Type" = 2 THEN TRUE ELSE FALSE END
                """);

            migrationBuilder.AlterColumn<bool>(
                name: "IsGroup",
                table: "Conversations",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Conversations");
        }
    }
}
