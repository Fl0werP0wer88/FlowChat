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
                name: "ContactObserverReadModel",
                columns: table => new
                {
                    ObservedUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ObserverUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsBlocked = table.Column<bool>(type: "boolean", nullable: false),
                    SourceVersion = table.Column<int>(type: "integer", nullable: false),
                    SourceCreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SourceLastModifiedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SourceDeletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, computedColumnSql: "\"SourceDeletedAtUtc\"", stored: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContactObserverReadModel", x => new { x.ObservedUserId, x.ObserverUserId });
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

            migrationBuilder.CreateTable(
                name: "SilverbackStoredOffsets",
                columns: table => new
                {
                    GroupId = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Topic = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Partition = table.Column<int>(type: "integer", nullable: false),
                    Offset = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SilverbackStoredOffsets", x => new { x.GroupId, x.Topic, x.Partition });
                });

            migrationBuilder.CreateTable(
                name: "UserPresencePreferences",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PreferredStatus = table.Column<int>(type: "integer", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    LastModifiedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPresencePreferences", x => x.UserId);
                });

            migrationBuilder.CreateIndex(
                name: "ix_contact_observer_projection_observed_user_id",
                table: "ContactObserverReadModel",
                column: "ObservedUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContactObserverReadModel");

            migrationBuilder.DropTable(
                name: "SilverbackOutboxMessages");

            migrationBuilder.DropTable(
                name: "SilverbackStoredOffsets");

            migrationBuilder.DropTable(
                name: "UserPresencePreferences");
        }
    }
}
