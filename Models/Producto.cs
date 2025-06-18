using ProyectoIdentity.Models;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProyectoIdentity.Models
{
    public class Producto
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string? Descripcion { get; set; }
        public string? Categoria { get; set; }
        public int? Cantidad { get; set; }
        public decimal Precio { get; set; }
        public decimal? Total { get; set; }

        // Nuevos campos para información nutricional y alérgenos
        public string? InfoNutricional { get; set; }
        public string? Alergenos { get; set; }

        // Relación con Pedido
        public int? PedidoId { get; set; }
        public Pedido? Pedido { get; set; }

        public byte[]? Imagen { get; set; }

        public string? Ingredientes { get; set; }  // JSON con lista de ingredientes


        // Relación con PedidoProducto
        public ICollection<PedidoProducto>? PedidoProductos { get; set; }

        public ICollection<DetallePedido> DetallesPedido { get; set; } = new List<DetallePedido>();


        [NotMapped]
        public bool Seleccionado { get; set; }

    }

}
