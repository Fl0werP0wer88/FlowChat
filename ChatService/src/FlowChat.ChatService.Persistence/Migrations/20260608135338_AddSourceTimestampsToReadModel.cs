using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlowChat.ChatService.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSourceTimestampsToReadModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SourceCreatedAtUtc",
                table: "UserProfileReadModel",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SourceDeletedAtUtc",
                table: "UserProfileReadModel",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SourceLastModifiedAtUtc",
                table: "UserProfileReadModel",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SourceCreatedAtUtc",
                table: "UserProfileReadModel");

            migrationBuilder.DropColumn(
                name: "SourceDeletedAtUtc",
                table: "UserProfileReadModel");

            migrationBuilder.DropColumn(
                name: "SourceLastModifiedAtUtc",
                table: "UserProfileReadModel");
        }
    }
}
