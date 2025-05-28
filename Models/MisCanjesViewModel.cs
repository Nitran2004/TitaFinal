namespace ProyectoIdentity.Models
{
    public class MisCanjesViewModel
    {
        public AppUsuario Usuario { get; set; }
        public List<CanjeAgrupadoViewModel> CanjesAgrupados { get; set; } = new List<CanjeAgrupadoViewModel>();
        public int PuntosActuales { get; set; }
    }
}
