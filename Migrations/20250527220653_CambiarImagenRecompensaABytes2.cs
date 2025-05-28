using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProyectoIdentity.Migrations
{
    public partial class CambiarImagenRecompensaABytes2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Agregar nueva columna temporal
            migrationBuilder.AddColumn<byte[]>(
                name: "ImagenBytes",
                table: "ProductosRecompensa",
                type: "varbinary(max)",
                nullable: true);

            // Eliminar la columna antigua
            migrationBuilder.DropColumn(
                name: "Imagen",
                table: "ProductosRecompensa");

            // Renombrar la nueva columna
            migrationBuilder.RenameColumn(
                name: "ImagenBytes",
                table: "ProductosRecompensa",
                newName: "Imagen");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revertir cambios
            migrationBuilder.AddColumn<string>(
                name: "ImagenString",
                table: "ProductosRecompensa",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.DropColumn(
                name: "Imagen",
                table: "ProductosRecompensa");

            migrationBuilder.RenameColumn(
                name: "ImagenString",
                table: "ProductosRecompensa",
                newName: "Imagen");
        }
    }
}
