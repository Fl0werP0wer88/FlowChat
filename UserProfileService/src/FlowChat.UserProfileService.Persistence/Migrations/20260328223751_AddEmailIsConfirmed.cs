using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlowChat.UserProfileService.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailIsConfirmed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsConfirmed",
                table: "Emails",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsConfirmed",
                table: "Emails");
        }
    }
}
