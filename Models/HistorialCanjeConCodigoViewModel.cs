namespace ProyectoIdentity.Models
{
    public class HistorialCanjeConCodigoViewModel
    {
        public int Id { get; set; }
        public List<HistorialCanje> CanjesDelGrupo { get; set; } = new List<HistorialCanje>();
        public int TotalPuntosUtilizados { get; set; }
        public int CantidadRecompensas { get; set; }
        public DateTime FechaCanje { get; set; }
        public string CodigoCanje { get; set; }
        public bool EsCanjeMultiple { get; set; }
        public string EstadoCanje { get; set; } = "Completado";
    }
}
