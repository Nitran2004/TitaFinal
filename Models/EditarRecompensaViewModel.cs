namespace ProyectoIdentity.Models
{
    public class EditarRecompensaViewModel
    {
        public int Id { get; set; }
        public int? ProductoId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public decimal PrecioOriginal { get; set; }
        public int PuntosNecesarios { get; set; }
        public string Categoria { get; set; } = string.Empty;
        public byte[]? ImagenExistente { get; set; }
        public List<Producto> ProductosDisponibles { get; set; } = new();
    }
}