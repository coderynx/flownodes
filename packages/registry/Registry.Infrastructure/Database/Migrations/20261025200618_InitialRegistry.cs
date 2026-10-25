using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Registry.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class InitialRegistry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "namespaces",
                columns: table => new
                {
                    Name = table.Column<string>(type: "character varying(63)", maxLength: 63, nullable: false),
                    Uid = table.Column<Guid>(type: "uuid", nullable: false),
                    Generation = table.Column<long>(type: "bigint", nullable: false),
                    Seq = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_namespaces", x => x.Name);
                });

            migrationBuilder.CreateTable(
                name: "revisions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    Namespace = table.Column<string>(type: "character varying(63)", maxLength: 63, nullable: false),
                    Kind = table.Column<string>(type: "character varying(63)", maxLength: 63, nullable: false),
                    Name = table.Column<string>(type: "character varying(63)", maxLength: 63, nullable: false),
                    Uid = table.Column<Guid>(type: "uuid", nullable: false),
                    Generation = table.Column<long>(type: "bigint", nullable: false),
                    Spec = table.Column<string>(type: "jsonb", nullable: true),
                    Deleted = table.Column<bool>(type: "boolean", nullable: false),
                    AppliedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    author_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    author_username = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_revisions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "secret_values",
                columns: table => new
                {
                    Namespace = table.Column<string>(type: "character varying(63)", maxLength: 63, nullable: false),
                    Secret = table.Column<string>(type: "character varying(63)", maxLength: 63, nullable: false),
                    Key = table.Column<string>(type: "character varying(253)", maxLength: 253, nullable: false),
                    Encrypted = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_secret_values", x => new { x.Namespace, x.Secret, x.Key });
                });

            migrationBuilder.CreateTable(
                name: "user_access",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Username = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    IsDisabled = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    IsAdministrator = table.Column<bool>(type: "boolean", nullable: false),
                    LastSeq = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_access", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "resources",
                columns: table => new
                {
                    Namespace = table.Column<string>(type: "character varying(63)", maxLength: 63, nullable: false),
                    Kind = table.Column<string>(type: "character varying(63)", maxLength: 63, nullable: false),
                    Name = table.Column<string>(type: "character varying(63)", maxLength: 63, nullable: false),
                    Uid = table.Column<Guid>(type: "uuid", nullable: false),
                    Spec = table.Column<string>(type: "jsonb", nullable: false),
                    Generation = table.Column<long>(type: "bigint", nullable: false),
                    Seq = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<string>(type: "jsonb", nullable: true),
                    ObservedGeneration = table.Column<long>(type: "bigint", nullable: true),
                    StatusSeq = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_resources", x => new { x.Namespace, x.Kind, x.Name });
                    table.ForeignKey(
                        name: "FK_resources_namespaces_Namespace",
                        column: x => x.Namespace,
                        principalTable: "namespaces",
                        principalColumn: "Name",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "user_access_grants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Namespace = table.Column<string>(type: "character varying(63)", maxLength: 63, nullable: false),
                    Role = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_access_grants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_user_access_grants_user_access_UserId",
                        column: x => x.UserId,
                        principalTable: "user_access",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_namespaces_Uid",
                table: "namespaces",
                column: "Uid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_resources_Uid",
                table: "resources",
                column: "Uid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_revisions_Namespace_Kind_Name_Id",
                table: "revisions",
                columns: new[] { "Namespace", "Kind", "Name", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_user_access_grants_UserId",
                table: "user_access_grants",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "resources");

            migrationBuilder.DropTable(
                name: "revisions");

            migrationBuilder.DropTable(
                name: "secret_values");

            migrationBuilder.DropTable(
                name: "user_access_grants");

            migrationBuilder.DropTable(
                name: "namespaces");

            migrationBuilder.DropTable(
                name: "user_access");
        }
    }
}
