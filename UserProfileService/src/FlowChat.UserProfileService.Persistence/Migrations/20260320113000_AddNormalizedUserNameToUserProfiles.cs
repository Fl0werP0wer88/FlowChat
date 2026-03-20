using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlowChat.UserProfileService.Persistence.Migrations;

public partial class AddNormalizedUserNameToUserProfiles : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "NormalizedUserName",
            table: "UserProfiles",
            type: "character varying(100)",
            maxLength: 100,
            nullable: true);

        migrationBuilder.Sql("""
            UPDATE "UserProfiles"
            SET "NormalizedUserName" = UPPER(BTRIM("UserName"))
            WHERE "NormalizedUserName" IS NULL;
            """);

        migrationBuilder.AlterColumn<string>(
            name: "NormalizedUserName",
            table: "UserProfiles",
            type: "character varying(100)",
            maxLength: 100,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(100)",
            oldMaxLength: 100,
            oldNullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "NormalizedUserName",
            table: "UserProfiles");
    }
}
