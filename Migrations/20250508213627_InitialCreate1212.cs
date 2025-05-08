using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProyectoIdentity.Migrations
{
    public partial class InitialCreate1212 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProductosRecompensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductoId = table.Column<int>(type: "int", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PrecioOriginal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PuntosNecesarios = table.Column<int>(type: "int", nullable: false),
                    Imagen = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Categoria = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductosRecompensa", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductosRecompensa_Productos_ProductoId",
                        column: x => x.ProductoId,
                        principalTable: "Productos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UsuarioPuntos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UsuarioId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PuntosAcumulados = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsuarioPuntos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UsuarioPuntos_AspNetUsers_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HistorialCanjes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UsuarioId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProductoRecompensaId = table.Column<int>(type: "int", nullable: false),
                    PuntosCanjeados = table.Column<int>(type: "int", nullable: false),
                    FechaCanje = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistorialCanjes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HistorialCanjes_AspNetUsers_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HistorialCanjes_ProductosRecompensa_ProductoRecompensaId",
                        column: x => x.ProductoRecompensaId,
                        principalTable: "ProductosRecompensa",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HistorialCanjes_ProductoRecompensaId",
                table: "HistorialCanjes",
                column: "ProductoRecompensaId");

            migrationBuilder.CreateIndex(
                name: "IX_HistorialCanjes_UsuarioId",
                table: "HistorialCanjes",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductosRecompensa_ProductoId",
                table: "ProductosRecompensa",
                column: "ProductoId");

            migrationBuilder.CreateIndex(
                name: "IX_UsuarioPuntos_UsuarioId",
                table: "UsuarioPuntos",
                column: "UsuarioId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HistorialCanjes");

            migrationBuilder.DropTable(
                name: "UsuarioPuntos");

            migrationBuilder.DropTable(
                name: "ProductosRecompensa");
        }
    }
}
