using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlowChat.SocialGraphService.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserProfileProjectionContactMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "MainEmailIsConfirmed",
                table: "UserProfileProjection",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "MainEmailIsVisible",
                table: "UserProfileProjection",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "MainPhoneIsConfirmed",
                table: "UserProfileProjection",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "MainPhoneIsVisible",
                table: "UserProfileProjection",
                type: "boolean",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MainEmailIsConfirmed",
                table: "UserProfileProjection");

            migrationBuilder.DropColumn(
                name: "MainEmailIsVisible",
                table: "UserProfileProjection");

            migrationBuilder.DropColumn(
                name: "MainPhoneIsConfirmed",
                table: "UserProfileProjection");

            migrationBuilder.DropColumn(
                name: "MainPhoneIsVisible",
                table: "UserProfileProjection");
        }
    }
}
