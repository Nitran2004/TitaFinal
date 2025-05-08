using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProyectoIdentity.Migrations
{
    public partial class InitialMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ItemPedido_Pedidos_PedidoId",
                table: "ItemPedido");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ItemPedido",
                table: "ItemPedido");

            migrationBuilder.RenameTable(
                name: "ItemPedido",
                newName: "ItemsPedido");

            migrationBuilder.RenameIndex(
                name: "IX_ItemPedido_PedidoId",
                table: "ItemsPedido",
                newName: "IX_ItemsPedido_PedidoId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ItemsPedido",
                table: "ItemsPedido",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ItemsPedido_Pedidos_PedidoId",
                table: "ItemsPedido",
                column: "PedidoId",
                principalTable: "Pedidos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ItemsPedido_Pedidos_PedidoId",
                table: "ItemsPedido");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ItemsPedido",
                table: "ItemsPedido");

            migrationBuilder.RenameTable(
                name: "ItemsPedido",
                newName: "ItemPedido");

            migrationBuilder.RenameIndex(
                name: "IX_ItemsPedido_PedidoId",
                table: "ItemPedido",
                newName: "IX_ItemPedido_PedidoId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ItemPedido",
                table: "ItemPedido",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ItemPedido_Pedidos_PedidoId",
                table: "ItemPedido",
                column: "PedidoId",
                principalTable: "Pedidos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
