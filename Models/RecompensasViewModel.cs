namespace ProyectoIdentity.Models
{
    public class RecompensasViewModel
    {
        public int PuntosUsuario { get; set; }
        public List<ProductoRecompensa> ProductosRecompensa { get; set; } = new List<ProductoRecompensa>();
    }
}
