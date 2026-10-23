using System;
using Microsoft.EntityFrameworkCore.Migrations;

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
                name: "resources",
                columns: table => new
                {
                    Namespace = table.Column<string>(type: "character varying(63)", maxLength: 63, nullable: false),
                    Kind = table.Column<string>(type: "character varying(63)", maxLength: 63, nullable: false),
                    Name = table.Column<string>(type: "character varying(253)", maxLength: 253, nullable: false)
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

            migrationBuilder.CreateIndex(
                name: "IX_namespaces_Uid",
                table: "namespaces",
                column: "Uid",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "resources");

            migrationBuilder.DropTable(
                name: "namespaces");
        }
    }
}
