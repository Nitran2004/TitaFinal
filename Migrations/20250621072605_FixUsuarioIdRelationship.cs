using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProyectoIdentity.Migrations
{
    public partial class FixUsuarioIdRelationship : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PedidoDetalles_Pedidos_PedidoId",
                table: "PedidoDetalles");

            migrationBuilder.DropForeignKey(
                name: "FK_PedidoDetalles_Productos_ProductoId",
                table: "PedidoDetalles");

            migrationBuilder.DropForeignKey(
                name: "FK_Pedidos_CollectionPoints_PuntoRecoleccionId",
                table: "Pedidos");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PedidoDetalles",
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

            migrationBuilder.AlterColumn<string>(
                name: "InfoNutricional",
                table: "Productos",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Alergenos",
                table: "Productos",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TipoServicio",
                table: "Pedidos",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Estado",
                table: "Pedidos",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                defaultValue: "Preparándose",
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true,
                oldDefaultValue: "Pendiente");

            migrationBuilder.AlterColumn<string>(
                name: "Comentario",
                table: "Pedidos",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "NotasEspeciales",
                table: "PedidoDetalle",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "IngredientesRemovidos",
                table: "PedidoDetalle",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_PedidoDetalle",
                table: "PedidoDetalle",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "Valoraciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PedidoId = table.Column<int>(type: "int", nullable: false),
                    UsuarioId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ValoracionGeneral = table.Column<int>(type: "int", nullable: false),
                    ValoracionCalidad = table.Column<int>(type: "int", nullable: false),
                    ValoracionTiempo = table.Column<int>(type: "int", nullable: false),
                    Comentarios = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Valoraciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Valoraciones_AspNetUsers_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Valoraciones_Pedidos_PedidoId",
                        column: x => x.PedidoId,
                        principalTable: "Pedidos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Valoraciones_PedidoId",
                table: "Valoraciones",
                column: "PedidoId");

            migrationBuilder.CreateIndex(
                name: "IX_Valoraciones_UsuarioId",
                table: "Valoraciones",
                column: "UsuarioId");

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
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Pedidos_CollectionPoints_PuntoRecoleccionId",
                table: "Pedidos",
                column: "PuntoRecoleccionId",
                principalTable: "CollectionPoints",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PedidoDetalle_Pedidos_PedidoId",
                table: "PedidoDetalle");

            migrationBuilder.DropForeignKey(
                name: "FK_PedidoDetalle_Productos_ProductoId",
                table: "PedidoDetalle");

            migrationBuilder.DropForeignKey(
                name: "FK_Pedidos_CollectionPoints_PuntoRecoleccionId",
                table: "Pedidos");

            migrationBuilder.DropTable(
                name: "Valoraciones");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PedidoDetalle",
                table: "PedidoDetalle");

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

            migrationBuilder.AlterColumn<string>(
                name: "InfoNutricional",
                table: "Productos",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Alergenos",
                table: "Productos",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TipoServicio",
                table: "Pedidos",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Estado",
                table: "Pedidos",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                defaultValue: "Pendiente",
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true,
                oldDefaultValue: "Preparándose");

            migrationBuilder.AlterColumn<string>(
                name: "Comentario",
                table: "Pedidos",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "NotasEspeciales",
                table: "PedidoDetalles",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "IngredientesRemovidos",
                table: "PedidoDetalles",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_PedidoDetalles",
                table: "PedidoDetalles",
                column: "Id");

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

            migrationBuilder.AddForeignKey(
                name: "FK_Pedidos_CollectionPoints_PuntoRecoleccionId",
                table: "Pedidos",
                column: "PuntoRecoleccionId",
                principalTable: "CollectionPoints",
                principalColumn: "Id");
        }
    }
}
