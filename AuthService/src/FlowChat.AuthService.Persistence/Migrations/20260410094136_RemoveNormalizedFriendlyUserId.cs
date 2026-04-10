using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlowChat.AuthService.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveNormalizedFriendlyUserId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Accounts_NormalizedFriendlyUserId",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "NormalizedFriendlyUserId",
                table: "Accounts");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NormalizedFriendlyUserId",
                table: "Accounts",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_NormalizedFriendlyUserId",
                table: "Accounts",
                column: "NormalizedFriendlyUserId",
                unique: true);
        }
    }
}
