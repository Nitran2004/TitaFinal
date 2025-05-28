namespace ProyectoIdentity.Models
{
    public class ResumenAdminCanjesViewModel
    {
        public List<CanjeAdminViewModel> CanjesAgrupados { get; set; } = new List<CanjeAdminViewModel>();
        public EstadisticasCanjesViewModel Estadisticas { get; set; }
    }
}
