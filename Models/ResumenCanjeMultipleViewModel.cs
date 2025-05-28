namespace ProyectoIdentity.Models
{
    public class ResumenCanjeMultipleViewModel
    {
        public AppUsuario Usuario { get; set; }
        public List<HistorialCanje> CanjesRealizados { get; set; }
        public int TotalPuntosUtilizados { get; set; }
        public int PuntosRestantes { get; set; }
        public DateTime FechaCanje { get; set; }
        public string CodigoCanje { get; set; }
        public List<RecompensaCanjeadaInfo> RecompensasCanjeadas { get; set; }
    }
}
