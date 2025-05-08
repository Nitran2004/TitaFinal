namespace ProyectoIdentity.Models
{
    public class ItemCarrito
    {
        public int Id { get; set; }
        public int CarritoId { get; set; }
        public int ProductoId { get; set; }
        public string TipoProducto { get; set; } // "Pizza", "Sanduche", etc.
        public string NombreProducto { get; set; }
        public decimal PrecioUnitario { get; set; }
        public int Cantidad { get; set; }
        public decimal SubTotal { get; set; }
    }
}
