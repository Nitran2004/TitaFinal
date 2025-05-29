namespace ProyectoIdentity.Models
{
    public class EscanearQRViewModel
    {
        public string CodigoQR { get; set; }
        public string UsuarioEscaneador { get; set; }
        public CuponViewModel CuponEncontrado { get; set; }
        public List<ProductoSeleccionado> ProductosSeleccionados { get; set; } = new List<ProductoSeleccionado>();
        public decimal TotalOriginal { get; set; }
        public decimal DescuentoCalculado { get; set; }
        public decimal TotalFinal { get; set; }
        public bool CuponValido { get; set; }
        public string MensajeError { get; set; }
    }
}
