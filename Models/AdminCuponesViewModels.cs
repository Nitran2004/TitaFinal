namespace ProyectoIdentity.Models
{
    public class DetalleCuponViewModel
    {
        public Cupon Cupon { get; set; }
        public int TotalCanjes { get; set; }
        public decimal TotalAhorrado { get; set; }
        public DateTime? UltimoUso { get; set; }
    }

    public class ActualizarFechaRequest
    {
        public int CuponId { get; set; }
        public string NuevaFecha { get; set; }
    }

    public class ToggleActivoRequest
    {
        public int CuponId { get; set; }
        public bool Activo { get; set; }
    }
}