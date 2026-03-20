using Microsoft.EntityFrameworkCore.Migrations;

namespace FlowChat.SocialGraphService.Persistence.Migrations;

[Migration("20260320110000_RemoveUserSocialGraphs")]
public partial class RemoveUserSocialGraphs : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "UserSocialGraphs");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "UserSocialGraphs",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                Login = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                PhoneNumber = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                IsPhoneVisible = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                IsEmailVisible = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                CreatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                CreatedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                LastModifiedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                LastModifiedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_UserSocialGraphs", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "uq_user_social_graph_user_id",
            table: "UserSocialGraphs",
            column: "UserId",
            unique: true);
    }
}
