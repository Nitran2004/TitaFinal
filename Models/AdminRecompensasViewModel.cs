namespace ProyectoIdentity.Models
{
    public class AdminRecompensasViewModel
    {
        public List<ProductoRecompensa> RecompensasActuales { get; set; } = new();
        public List<Producto> ProductosDisponibles { get; set; } = new();
        public int TotalRecompensas { get; set; }
        public decimal ValorTotalRecompensas { get; set; }
    }
}