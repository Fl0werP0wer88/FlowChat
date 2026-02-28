using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlowChat.SocialGraphService.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RenameContactColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "uq_contact_pair",
                table: "Contacts");

            migrationBuilder.DropCheckConstraint(
                name: "chk_different_users",
                table: "Contacts");

            migrationBuilder.DropCheckConstraint(
                name: "chk_user_order",
                table: "Contacts");

            migrationBuilder.RenameColumn(
                name: "LastModifiedDate",
                table: "Invitations",
                newName: "LastModifiedAtUtc");

            migrationBuilder.RenameColumn(
                name: "CreatedDate",
                table: "Invitations",
                newName: "CreatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "UserId1",
                table: "Contacts",
                newName: "OwnerUserId");

            migrationBuilder.RenameColumn(
                name: "UserId2",
                table: "Contacts",
                newName: "ContactUserId");

            migrationBuilder.RenameColumn(
                name: "LastModifiedDate",
                table: "Contacts",
                newName: "LastModifiedAtUtc");

            migrationBuilder.RenameColumn(
                name: "CreatedDate",
                table: "Contacts",
                newName: "CreatedAtUtc");

            migrationBuilder.DropColumn(
                name: "BlockedBy",
                table: "Contacts");

            migrationBuilder.AddColumn<Guid>(
                name: "UserSocialGraphId",
                table: "Contacts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UserSocialGraphId",
                table: "Invitations",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "UserSocialGraphs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: false),
                    LastModifiedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSocialGraphs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Contacts_UserSocialGraphId",
                table: "Contacts",
                column: "UserSocialGraphId");

            migrationBuilder.CreateIndex(
                name: "IX_Invitations_UserSocialGraphId",
                table: "Invitations",
                column: "UserSocialGraphId");

            migrationBuilder.CreateIndex(
                name: "uq_contact_owner_contact",
                table: "Contacts",
                columns: new[] { "OwnerUserId", "ContactUserId" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "chk_different_users",
                table: "Contacts",
                sql: "\"OwnerUserId\" <> \"ContactUserId\"");

            migrationBuilder.CreateIndex(
                name: "uq_user_social_graph_user_id",
                table: "UserSocialGraphs",
                column: "UserId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Contacts_UserSocialGraphs_UserSocialGraphId",
                table: "Contacts",
                column: "UserSocialGraphId",
                principalTable: "UserSocialGraphs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Invitations_UserSocialGraphs_UserSocialGraphId",
                table: "Invitations",
                column: "UserSocialGraphId",
                principalTable: "UserSocialGraphs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Contacts_UserSocialGraphs_UserSocialGraphId",
                table: "Contacts");

            migrationBuilder.DropForeignKey(
                name: "FK_Invitations_UserSocialGraphs_UserSocialGraphId",
                table: "Invitations");

            migrationBuilder.DropTable(
                name: "UserSocialGraphs");

            migrationBuilder.DropIndex(
                name: "IX_Invitations_UserSocialGraphId",
                table: "Invitations");

            migrationBuilder.DropIndex(
                name: "IX_Contacts_UserSocialGraphId",
                table: "Contacts");

            migrationBuilder.DropIndex(
                name: "uq_contact_owner_contact",
                table: "Contacts");

            migrationBuilder.DropCheckConstraint(
                name: "chk_different_users",
                table: "Contacts");

            migrationBuilder.DropColumn(
                name: "UserSocialGraphId",
                table: "Contacts");

            migrationBuilder.DropColumn(
                name: "UserSocialGraphId",
                table: "Invitations");

            migrationBuilder.RenameColumn(
                name: "LastModifiedAtUtc",
                table: "Invitations",
                newName: "LastModifiedDate");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                table: "Invitations",
                newName: "CreatedDate");

            migrationBuilder.RenameColumn(
                name: "ContactUserId",
                table: "Contacts",
                newName: "UserId2");

            migrationBuilder.RenameColumn(
                name: "OwnerUserId",
                table: "Contacts",
                newName: "UserId1");

            migrationBuilder.RenameColumn(
                name: "LastModifiedAtUtc",
                table: "Contacts",
                newName: "LastModifiedDate");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                table: "Contacts",
                newName: "CreatedDate");

            migrationBuilder.AddColumn<Guid>(
                name: "BlockedBy",
                table: "Contacts",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "uq_contact_pair",
                table: "Contacts",
                columns: new[] { "UserId1", "UserId2" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "chk_different_users",
                table: "Contacts",
                sql: "\"UserId1\" <> \"UserId2\"");

            migrationBuilder.AddCheckConstraint(
                name: "chk_user_order",
                table: "Contacts",
                sql: "\"UserId1\" < \"UserId2\"");
        }
    }
}
