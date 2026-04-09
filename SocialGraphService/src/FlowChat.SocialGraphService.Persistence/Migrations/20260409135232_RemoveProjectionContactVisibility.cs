using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlowChat.SocialGraphService.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveProjectionContactVisibility : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsEmailVisible",
                table: "UserProfileProjection");

            migrationBuilder.DropColumn(
                name: "IsPhoneVisible",
                table: "UserProfileProjection");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsEmailVisible",
                table: "UserProfileProjection",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsPhoneVisible",
                table: "UserProfileProjection",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
