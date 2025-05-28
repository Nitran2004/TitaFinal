namespace ProyectoIdentity.Models
{
    public class AdminCanjesDetalleViewModel
    {
        public AppUsuario Usuario { get; set; }
        public List<CanjeDetalleViewModel> HistorialCanjes { get; set; } = new List<CanjeDetalleViewModel>();
        public int TotalCanjes { get; set; }
        public int TotalPuntosUtilizados { get; set; }
        public decimal TotalValorAhorrado { get; set; }
    }
}
