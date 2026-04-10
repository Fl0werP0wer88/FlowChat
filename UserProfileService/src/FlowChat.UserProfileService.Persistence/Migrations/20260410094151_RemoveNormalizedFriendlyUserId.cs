using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlowChat.UserProfileService.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveNormalizedFriendlyUserId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NormalizedUserName",
                table: "UserProfiles");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NormalizedUserName",
                table: "UserProfiles",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }
    }
}
