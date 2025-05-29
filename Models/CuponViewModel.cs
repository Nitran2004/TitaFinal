namespace ProyectoIdentity.Models
{
    public class CuponViewModel
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public string TipoPromocion { get; set; }
        public string CodigoQR { get; set; }
        public byte[]? ImagenCupon { get; set; }
        public bool EsValidoHoy { get; set; }
        public string DiasAplicables { get; set; }
        public string TextoDescuento { get; set; } // "3x2", "50% OFF", etc.
        public string ProductosNombres { get; set; } // Nombres de productos aplicables
        public DateTime FechaFin { get; set; }
    }
}
