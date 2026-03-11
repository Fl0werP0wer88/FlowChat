using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlowChat.SocialGraphService.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserProfileReadModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserProfileReadModel",
                columns: table => new
                {
                    UserProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    MainEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    MainPhone = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    AvatarUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    Bio = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    LastSeenAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsEmailVisible = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    IsPhoneVisible = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: false),
                    LastModifiedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserProfileReadModel", x => x.UserProfileId);
                });

            migrationBuilder.CreateIndex(
                name: "ix_user_profile_read_model_user_name",
                table: "UserProfileReadModel",
                column: "UserName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserProfileReadModel");
        }
    }
}
