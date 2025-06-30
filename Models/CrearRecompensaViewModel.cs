namespace ProyectoIdentity.Models
{
    public class CrearRecompensaViewModel
    {
        public int ProductoId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public decimal PrecioOriginal { get; set; }
        public int PuntosNecesarios { get; set; }
        public string Categoria { get; set; } = string.Empty;
        public List<Producto> ProductosDisponibles { get; set; } = new();
    }
}