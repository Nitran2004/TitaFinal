namespace ProyectoIdentity.Models
{
    public class ResumenCanjesCompletosViewModel
    {
        public AppUsuario Usuario { get; set; }
        public List<GrupoCanjeViewModel> GruposCanjes { get; set; } = new List<GrupoCanjeViewModel>();
        public int PuntosRestantes { get; set; }
    }
}
