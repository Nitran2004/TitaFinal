// Crea un archivo en Migrations/ con este nombre: [fecha]_CorregirImagenCupon.cs

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProyectoIdentity.Migrations
{
    public partial class CorregirImagenCupon : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Eliminar la columna existente (byte[])
            migrationBuilder.DropColumn(
                name: "ImagenCupon",
                table: "Cupones");

            // Agregar la nueva columna como string
            migrationBuilder.AddColumn<string>(
                name: "ImagenCupon",
                table: "Cupones",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImagenCupon",
                table: "Cupones");

            migrationBuilder.AddColumn<byte[]>(
                name: "ImagenCupon",
                table: "Cupones",
                type: "varbinary(max)",
                nullable: true);
        }
    }
}