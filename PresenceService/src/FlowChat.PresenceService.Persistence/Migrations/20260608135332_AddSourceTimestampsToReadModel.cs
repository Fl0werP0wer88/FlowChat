using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlowChat.PresenceService.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSourceTimestampsToReadModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SourceCreatedAtUtc",
                table: "ContactObserverReadModel",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SourceDeletedAtUtc",
                table: "ContactObserverReadModel",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SourceLastModifiedAtUtc",
                table: "ContactObserverReadModel",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SourceCreatedAtUtc",
                table: "ContactObserverReadModel");

            migrationBuilder.DropColumn(
                name: "SourceDeletedAtUtc",
                table: "ContactObserverReadModel");

            migrationBuilder.DropColumn(
                name: "SourceLastModifiedAtUtc",
                table: "ContactObserverReadModel");
        }
    }
}
