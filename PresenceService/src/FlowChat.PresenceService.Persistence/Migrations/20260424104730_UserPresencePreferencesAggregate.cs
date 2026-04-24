using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlowChat.PresenceService.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UserPresencePreferencesAggregate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAtUtc",
                table: "UserPresencePreferences",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "UserPresencePreferences",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "system");

            migrationBuilder.AddColumn<string>(
                name: "LastModifiedBy",
                table: "UserPresencePreferences",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "system");

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "UserPresencePreferences",
                type: "integer",
                nullable: false,
                defaultValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "UserPresencePreferences");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "UserPresencePreferences");

            migrationBuilder.DropColumn(
                name: "LastModifiedBy",
                table: "UserPresencePreferences");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "UserPresencePreferences");
        }
    }
}
