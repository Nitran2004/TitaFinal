using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProyectoIdentity.Migrations
{
    public partial class LimpiarRecompensas : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Eliminar todos los datos existentes
            migrationBuilder.Sql("DELETE FROM ProductosRecompensa");

            // Eliminar la columna Imagen actual (nvarchar)
            migrationBuilder.DropColumn(
                name: "Imagen",
                table: "ProductosRecompensa");

            // Agregar nueva columna Imagen como varbinary(max)
            migrationBuilder.AddColumn<byte[]>(
                name: "Imagen",
                table: "ProductosRecompensa",
                type: "varbinary(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Imagen",
                table: "ProductosRecompensa");

            migrationBuilder.AddColumn<string>(
                name: "Imagen",
                table: "ProductosRecompensa",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
