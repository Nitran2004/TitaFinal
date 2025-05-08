namespace ProyectoIdentity.Models
{ 
    public class Pedido
    {
        public int Id { get; set; }
        public string? UsuarioId { get; set; } // Puede ser null si no se ha registrado
        public ICollection<PedidoProducto>? PedidoProductos { get; set; }
    }

    
}
