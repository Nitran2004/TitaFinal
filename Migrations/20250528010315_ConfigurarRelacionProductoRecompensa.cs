using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProyectoIdentity.Migrations
{
    public partial class ConfigurarRelacionProductoRecompensa : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductosRecompensa_Productos_ProductoId",
                table: "ProductosRecompensa");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductosRecompensa_Productos_ProductoId",
                table: "ProductosRecompensa",
                column: "ProductoId",
                principalTable: "Productos",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductosRecompensa_Productos_ProductoId",
                table: "ProductosRecompensa");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductosRecompensa_Productos_ProductoId",
                table: "ProductosRecompensa",
                column: "ProductoId",
                principalTable: "Productos",
                principalColumn: "Id");
        }
    }
}
