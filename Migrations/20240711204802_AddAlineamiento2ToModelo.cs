using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProyectoIdentity.Migrations
{
    public partial class AddAlineamiento2ToModelo : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Alineamiento2",
                table: "Modelos",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Alineamiento2",
                table: "Modelos");
        }
    }
}
