namespace ProyectoIdentity.Models
{
    public class ProductoVentaViewModel
    {
        public int ProductoId { get; set; }
        public int Cantidad { get; set; }
        public decimal Precio { get; set; }
        public decimal Subtotal { get; set; }
        public string? Nombre { get; set; }
        public string? Categoria { get; set; }
    }
}
