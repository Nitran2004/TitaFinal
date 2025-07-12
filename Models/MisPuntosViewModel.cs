namespace ProyectoIdentity.Models
{
    public class MisPuntosViewModel
    {
        public int PuntosActuales { get; set; }
        public AppUsuario Usuario { get; set; } = new AppUsuario();
        public List<TransaccionPuntos> UltimasTransacciones { get; set; } = new List<TransaccionPuntos>();
    }
}
