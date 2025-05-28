namespace ProyectoIdentity.Models
{
    public class CanjeDetalleViewModel
    {
        public int Id { get; set; }
        public int ProductoRecompensaId { get; set; }
        public string NombreProducto { get; set; }
        public string CategoriaProducto { get; set; }
        public int PuntosUtilizados { get; set; }
        public DateTime FechaCanje { get; set; }
        public decimal PrecioOriginal { get; set; }
        public string CodigoCanje { get; set; }
    }
}
