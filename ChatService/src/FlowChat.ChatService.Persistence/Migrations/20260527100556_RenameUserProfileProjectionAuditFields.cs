using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlowChat.ChatService.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RenameUserProfileProjectionAuditFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UpdatedAtUtc",
                table: "UserProfileProjections",
                newName: "LastModifiedAtUtc");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAtUtc",
                table: "UserProfileProjections",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "UserProfileProjections",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LastModifiedBy",
                table: "UserProfileProjections",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(
                """
                UPDATE "UserProfileProjections"
                SET "CreatedAtUtc" = "LastModifiedAtUtc",
                    "CreatedBy" = 'migration',
                    "LastModifiedBy" = 'migration'
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "UserProfileProjections");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "UserProfileProjections");

            migrationBuilder.DropColumn(
                name: "LastModifiedBy",
                table: "UserProfileProjections");

            migrationBuilder.RenameColumn(
                name: "LastModifiedAtUtc",
                table: "UserProfileProjections",
                newName: "UpdatedAtUtc");
        }
    }
}
