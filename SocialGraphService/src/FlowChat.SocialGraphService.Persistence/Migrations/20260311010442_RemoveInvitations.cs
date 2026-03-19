using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlowChat.SocialGraphService.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveInvitations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Contacts_UserSocialGraphs_UserSocialGraphId",
                table: "Contacts");

            migrationBuilder.DropTable(
                name: "Invitations");

            migrationBuilder.DropIndex(
                name: "IX_Contacts_UserSocialGraphId",
                table: "Contacts");

            migrationBuilder.DropColumn(
                name: "UserSocialGraphId",
                table: "Contacts");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "UserSocialGraphId",
                table: "Contacts",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Invitations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserSocialGraphId = table.Column<Guid>(type: "uuid", nullable: true),
                    AddresseeId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    LastModifiedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: false),
                    RequesterId = table.Column<Guid>(type: "uuid", nullable: false),
                    RespondedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Invitations", x => x.Id);
                    table.CheckConstraint("chk_invitation_different_users", "\"RequesterId\" <> \"AddresseeId\"");
                    table.ForeignKey(
                        name: "FK_Invitations_UserSocialGraphs_UserSocialGraphId",
                        column: x => x.UserSocialGraphId,
                        principalTable: "UserSocialGraphs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Contacts_UserSocialGraphId",
                table: "Contacts",
                column: "UserSocialGraphId");

            migrationBuilder.CreateIndex(
                name: "ix_invitations_requester_addressee_status",
                table: "Invitations",
                columns: new[] { "RequesterId", "AddresseeId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Invitations_UserSocialGraphId",
                table: "Invitations",
                column: "UserSocialGraphId");

            migrationBuilder.AddForeignKey(
                name: "FK_Contacts_UserSocialGraphs_UserSocialGraphId",
                table: "Contacts",
                column: "UserSocialGraphId",
                principalTable: "UserSocialGraphs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
