using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlowChat.PresenceService.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSourceVersionToContactObserverProjection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SourceVersion",
                table: "ContactObserverReadModel",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SourceVersion",
                table: "ContactObserverReadModel");
        }
    }
}
