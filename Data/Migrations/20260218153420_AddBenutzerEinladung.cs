using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ArgeKassenbuch.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBenutzerEinladung : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BenutzerEinladungen",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BenutzerId = table.Column<int>(type: "integer", nullable: false),
                    Token = table.Column<string>(type: "text", nullable: false),
                    ErstelltAm = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    GueltigBis = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Verwendet = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BenutzerEinladungen", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BenutzerEinladungen_Token",
                table: "BenutzerEinladungen",
                column: "Token",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BenutzerEinladungen");
        }
    }
}
