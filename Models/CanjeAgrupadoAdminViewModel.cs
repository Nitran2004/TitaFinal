namespace ProyectoIdentity.Models
{
    // Models/CanjeAgrupadoAdminViewModel.cs
    public class CanjeAgrupadoAdminViewModel
    {
        public string CodigoCanje { get; set; }
        public DateTime FechaCanje { get; set; }
        public string? TipoServicio { get; set; }
        public string Estado { get; set; }
        public int TotalPuntosUtilizados { get; set; }
        public int CantidadRecompensas { get; set; }
        public List<CanjeDetalleViewModel> CanjesIndividuales { get; set; } = new();
        public decimal ValorTotalAhorrado { get; set; }
    }

    // Models/AdminCanjesDetalleAgrupadoViewModel.cs
    public class AdminCanjesDetalleAgrupadoViewModel
    {
        public AppUsuario Usuario { get; set; }
        public List<CanjeAgrupadoAdminViewModel> CanjesAgrupados { get; set; } = new();
        public int TotalCanjes { get; set; }
        public int TotalPuntosUtilizados { get; set; }
        public decimal TotalValorAhorrado { get; set; }
    }
}
