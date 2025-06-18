using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProyectoIdentity.Migrations
{
    public partial class ActualizarPedidoDetalle : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PedidoDetalle_Pedidos_PedidoId",
                table: "PedidoDetalle");

            migrationBuilder.DropForeignKey(
                name: "FK_PedidoDetalle_Productos_ProductoId",
                table: "PedidoDetalle");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PedidoDetalle",
                table: "PedidoDetalle");

            migrationBuilder.DropColumn(
                name: "LimiteUsos",
                table: "Cupones");

            migrationBuilder.RenameTable(
                name: "PedidoDetalle",
                newName: "PedidoDetalles");

            migrationBuilder.RenameIndex(
                name: "IX_PedidoDetalle_ProductoId",
                table: "PedidoDetalles",
                newName: "IX_PedidoDetalles_ProductoId");

            migrationBuilder.RenameIndex(
                name: "IX_PedidoDetalle_PedidoId",
                table: "PedidoDetalles",
                newName: "IX_PedidoDetalles_PedidoId");

            migrationBuilder.AddColumn<string>(
                name: "Ingredientes",
                table: "Productos",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TipoServicio",
                table: "HistorialCanjes",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "OtorgaPuntos",
                table: "Cupones",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "IngredientesRemovidos",
                table: "PedidoDetalles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NotasEspeciales",
                table: "PedidoDetalles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_PedidoDetalles",
                table: "PedidoDetalles",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "DetallePedido",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PedidoId = table.Column<int>(type: "int", nullable: false),
                    ProductoId = table.Column<int>(type: "int", nullable: false),
                    Cantidad = table.Column<int>(type: "int", nullable: false),
                    PrecioUnitario = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IngredientesRemovidos = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NotasEspeciales = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DetallePedido", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DetallePedido_Pedidos_PedidoId",
                        column: x => x.PedidoId,
                        principalTable: "Pedidos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DetallePedido_Productos_ProductoId",
                        column: x => x.ProductoId,
                        principalTable: "Productos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DetallePedido_PedidoId",
                table: "DetallePedido",
                column: "PedidoId");

            migrationBuilder.CreateIndex(
                name: "IX_DetallePedido_ProductoId",
                table: "DetallePedido",
                column: "ProductoId");

            migrationBuilder.AddForeignKey(
                name: "FK_PedidoDetalles_Pedidos_PedidoId",
                table: "PedidoDetalles",
                column: "PedidoId",
                principalTable: "Pedidos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PedidoDetalles_Productos_ProductoId",
                table: "PedidoDetalles",
                column: "ProductoId",
                principalTable: "Productos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PedidoDetalles_Pedidos_PedidoId",
                table: "PedidoDetalles");

            migrationBuilder.DropForeignKey(
                name: "FK_PedidoDetalles_Productos_ProductoId",
                table: "PedidoDetalles");

            migrationBuilder.DropTable(
                name: "DetallePedido");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PedidoDetalles",
                table: "PedidoDetalles");

            migrationBuilder.DropColumn(
                name: "Ingredientes",
                table: "Productos");

            migrationBuilder.DropColumn(
                name: "TipoServicio",
                table: "HistorialCanjes");

            migrationBuilder.DropColumn(
                name: "OtorgaPuntos",
                table: "Cupones");

            migrationBuilder.DropColumn(
                name: "IngredientesRemovidos",
                table: "PedidoDetalles");

            migrationBuilder.DropColumn(
                name: "NotasEspeciales",
                table: "PedidoDetalles");

            migrationBuilder.RenameTable(
                name: "PedidoDetalles",
                newName: "PedidoDetalle");

            migrationBuilder.RenameIndex(
                name: "IX_PedidoDetalles_ProductoId",
                table: "PedidoDetalle",
                newName: "IX_PedidoDetalle_ProductoId");

            migrationBuilder.RenameIndex(
                name: "IX_PedidoDetalles_PedidoId",
                table: "PedidoDetalle",
                newName: "IX_PedidoDetalle_PedidoId");

            migrationBuilder.AddColumn<int>(
                name: "LimiteUsos",
                table: "Cupones",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_PedidoDetalle",
                table: "PedidoDetalle",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PedidoDetalle_Pedidos_PedidoId",
                table: "PedidoDetalle",
                column: "PedidoId",
                principalTable: "Pedidos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PedidoDetalle_Productos_ProductoId",
                table: "PedidoDetalle",
                column: "ProductoId",
                principalTable: "Productos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
