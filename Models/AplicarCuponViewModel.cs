namespace ProyectoIdentity.Models
{
    public class AplicarCuponViewModel
    {
        public int CuponId { get; set; }
        public string CodigoQR { get; set; } = string.Empty;
        public int SucursalId { get; set; }
        public List<ProductoVentaViewModel> Productos { get; set; } = new List<ProductoVentaViewModel>();
        public decimal TotalOriginal { get; set; }
        public decimal DescuentoAplicado { get; set; }
        public decimal TotalConDescuento { get; set; }
    }
}
