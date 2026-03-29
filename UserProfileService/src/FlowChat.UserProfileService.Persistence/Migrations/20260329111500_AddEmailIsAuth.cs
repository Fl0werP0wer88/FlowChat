using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlowChat.UserProfileService.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailIsAuth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsAuth",
                table: "Emails",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.DropIndex(
                name: "uq_email_user_profile_address",
                table: "Emails");

            migrationBuilder.Sql(
                """
                UPDATE "Emails" AS e
                SET "IsAuth" = TRUE
                FROM (
                    SELECT DISTINCT ON ("UserProfileId") "Id"
                    FROM "Emails"
                    ORDER BY "UserProfileId", "IsMain" DESC, "CreatedAtUtc", "Id"
                ) AS selected
                WHERE e."Id" = selected."Id";
                """);

            migrationBuilder.CreateIndex(
                name: "uq_email_address",
                table: "Emails",
                column: "Address",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_email_user_profile_auth",
                table: "Emails",
                column: "UserProfileId",
                unique: true,
                filter: "\"IsAuth\" = TRUE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "uq_email_user_profile_auth",
                table: "Emails");

            migrationBuilder.DropIndex(
                name: "uq_email_address",
                table: "Emails");

            migrationBuilder.CreateIndex(
                name: "uq_email_user_profile_address",
                table: "Emails",
                columns: new[] { "UserProfileId", "Address" },
                unique: true);

            migrationBuilder.DropColumn(
                name: "IsAuth",
                table: "Emails");
        }
    }
}
