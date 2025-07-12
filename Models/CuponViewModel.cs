namespace ProyectoIdentity.Models
{
    public class CuponViewModel
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public string TipoPromocion { get; set; } = string.Empty;
        public string CodigoQR { get; set; } = string.Empty;
        public string? ImagenCupon { get; set; } // Cambiado a string
        public bool EsValidoHoy { get; set; }
        public string? DiasAplicables { get; set; }
        public string TextoDescuento { get; set; } = string.Empty;
        public string ProductosNombres { get; set; } = string.Empty;
        public DateTime FechaFin { get; set; }
    }
}
