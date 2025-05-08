namespace ProyectoIdentity.Models
{
    public class PedidoProducto
    {
        public int Id { get; set; }

        // Relación con Pedido
        public int PedidoId { get; set; }
        public Pedido? Pedido { get; set; }

        // Relación con Producto
        public int ProductoId { get; set; }
        public Producto? Producto { get; set; }

        // Atributos elegidos
        public int? Cantidad { get; set; }
        public decimal? Precio { get; set; }

        public decimal Total { get; set; }
    }

}
