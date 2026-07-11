using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlowChat.PresenceService.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddContactObserverIsBlocked : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsBlocked",
                table: "ContactObserverReadModel",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsBlocked",
                table: "ContactObserverReadModel");
        }
    }
}
