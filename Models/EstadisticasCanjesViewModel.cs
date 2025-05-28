namespace ProyectoIdentity.Models
{
    // ViewModel para estadísticas generales
    public class EstadisticasCanjesViewModel
    {
        public int TotalCanjesRealizados { get; set; }
        public int TotalRecompensasCanjeadas { get; set; }
        public int TotalPuntosUtilizados { get; set; }
        public decimal TotalValorAhorrado { get; set; }
        public int UsuariosUnicos { get; set; }
        public int CanjesHoy { get; set; }
        public int CanjesEsteMes { get; set; }
    }
}
