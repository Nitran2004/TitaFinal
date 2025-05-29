using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProyectoIdentity.Migrations
{
    public partial class AgregarCamposCuponesPedidos : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Cupones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TipoPromocion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DiasAplicables = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ProductosAplicables = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CategoriasAplicables = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DescuentoPorcentaje = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    DescuentoFijo = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    FechaInicio = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaFin = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    CodigoQR = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ImagenCupon = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cupones", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CuponesCanjeados",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CuponId = table.Column<int>(type: "int", nullable: false),
                    UsuarioId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    ClienteId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    FechaCanje = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CodigoQR = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DescuentoAplicado = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    ProductosIncluidos = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TotalOriginal = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    TotalConDescuento = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    EstadoCanje = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CuponesCanjeados", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CuponesCanjeados_AspNetUsers_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CuponesCanjeados_AspNetUsers_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CuponesCanjeados_Cupones_CuponId",
                        column: x => x.CuponId,
                        principalTable: "Cupones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Cupones_CodigoQR",
                table: "Cupones",
                column: "CodigoQR",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CuponesCanjeados_ClienteId",
                table: "CuponesCanjeados",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_CuponesCanjeados_CuponId",
                table: "CuponesCanjeados",
                column: "CuponId");

            migrationBuilder.CreateIndex(
                name: "IX_CuponesCanjeados_UsuarioId",
                table: "CuponesCanjeados",
                column: "UsuarioId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CuponesCanjeados");

            migrationBuilder.DropTable(
                name: "Cupones");
        }
    }
}
