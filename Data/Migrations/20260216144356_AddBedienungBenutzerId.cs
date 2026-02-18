using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArgeKassenbuch.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBedienungBenutzerId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BenutzerId",
                table: "Bedienungen",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BenutzerId",
                table: "Bedienungen");
        }
    }
}
