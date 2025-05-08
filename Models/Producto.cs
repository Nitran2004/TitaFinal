namespace ProyectoIdentity.Models
{
    public class Producto
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string? Descripcion { get; set; }
        public string? Categoria { get; set; }
        public int? Cantidad { get; set; }
        public decimal? Precio { get; set; }
        public decimal? Total { get; set; }

        // Nuevos campos para información nutricional y alérgenos
        public string? InfoNutricional { get; set; }
        public string? Alergenos { get; set; }

        // Relación con Pedido
        public int? PedidoId { get; set; }
        public Pedido? Pedido { get; set; }

        public byte[]? Imagen { get; set; }

        // Relación con PedidoProducto
        public ICollection<PedidoProducto>? PedidoProductos { get; set; }

    }

}
