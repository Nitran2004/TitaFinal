namespace ProyectoIdentity.Models
{
    public class AdminCanjesIndexViewModel
    {
        public List<UsuarioCanjesIndexViewModel> Usuarios { get; set; } = new List<UsuarioCanjesIndexViewModel>();
        public EstadisticasGeneralesViewModel Estadisticas { get; set; }
    }
}
