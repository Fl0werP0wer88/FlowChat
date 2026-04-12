using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FlowChat.PresenceService.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ContactObserverProjection",
                columns: table => new
                {
                    ObservedUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ObserverUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    LastModifiedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContactObserverProjection", x => new { x.ObservedUserId, x.ObserverUserId });
                    table.CheckConstraint("chk_contact_observer_projection_different_users", "\"ObservedUserId\" <> \"ObserverUserId\"");
                });

            migrationBuilder.CreateTable(
                name: "SilverbackOutboxMessages",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Content = table.Column<byte[]>(type: "bytea", nullable: false),
                    Headers = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    EndpointName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    DynamicEndpoint = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SilverbackOutboxMessages", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_contact_observer_projection_observed_user_id",
                table: "ContactObserverProjection",
                column: "ObservedUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContactObserverProjection");

            migrationBuilder.DropTable(
                name: "SilverbackOutboxMessages");
        }
    }
}
