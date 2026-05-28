using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlowChat.ChatService.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceDisplayNameWithFirstAndLastName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DisplayName",
                table: "UserProfileProjections");

            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                table: "UserProfileProjections",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastName",
                table: "UserProfileProjections",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FirstName",
                table: "UserProfileProjections");

            migrationBuilder.DropColumn(
                name: "LastName",
                table: "UserProfileProjections");

            migrationBuilder.AddColumn<string>(
                name: "DisplayName",
                table: "UserProfileProjections",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);
        }
    }
}
