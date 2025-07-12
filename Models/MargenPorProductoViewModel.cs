// ✅ CREAR ARCHIVO: Models/MargenPorProductoViewModel.cs

namespace ProyectoIdentity.Models
{
    public class MargenPorProductoViewModel
    {
        public decimal MargenTotalGeneral { get; set; }
        public int ProductosPersonalizados { get; set; }
        public int PeriodoDias { get; set; }

        public List<ProductoConMargenDto> ProductosConMargen { get; set; } = new();
    }

    public class ProductoConMargenDto
    {
        public string NombreProducto { get; set; } = "";
        public decimal PrecioOriginal { get; set; }
        public decimal CostoRealProduccion { get; set; }
        public decimal MargenExtra { get; set; }
        public int VecesPersonalizado { get; set; }
        public decimal MargenPromedioPorPedido { get; set; }
        public decimal PorcentajeMargenExtra { get; set; }

        public List<IngredienteQuitadoDetalle> IngredientesQuitados { get; set; } = new();
    }

    public class IngredienteQuitadoDetalle
    {
        public string Nombre { get; set; } = "";
        public decimal CostoUnitario { get; set; }
        public int VecesQuitado { get; set; }
        public decimal CostoTotal { get; set; }
    }
}