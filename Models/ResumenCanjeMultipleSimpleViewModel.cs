namespace ProyectoIdentity.Models
{
    public class ResumenCanjeMultipleSimpleViewModel
    {
        public AppUsuario Usuario { get; set; }
        public int TotalPuntosUtilizados { get; set; }
        public int CantidadRecompensas { get; set; }
        public int PuntosRestantes { get; set; }
        public DateTime FechaCanje { get; set; }
        public string CodigoCanje { get; set; }
    }
}
