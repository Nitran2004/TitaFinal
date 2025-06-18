namespace ProyectoIdentity.Models
{
    // Modelo simple para personalización
    public class PersonalizacionSimple
    {
        public int ProductoId { get; set; }
        public int Cantidad { get; set; } = 1;
        public List<string> IngredientesRemovidos { get; set; } = new();
        public string? NotasEspeciales { get; set; }
    }

    // Clase para análisis simple del admin
    public class AnalisisSimple
    {
        public string NombreIngrediente { get; set; } = "";
        public string NombreProducto { get; set; } = "";
        public decimal CostoUnitario { get; set; }
        public int VecesRemovido { get; set; }
        public decimal AhorroTotal { get; set; }
    }

    // AGREGAR ESTA CLASE:
    public class Ingrediente
    {
        public string Nombre { get; set; } = string.Empty;
        public decimal Costo { get; set; }
        public bool Removible { get; set; } = true;
    }

    public class PersonalizacionRequest
    {
        public int ProductoId { get; set; }
        public int Cantidad { get; set; } = 1;
        public List<string> IngredientesRemovidos { get; set; } = new();
        public string? NotasEspeciales { get; set; }
    }

    public class ItemCarritoPersonalizado
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = "";
        public decimal Precio { get; set; }
        public int Cantidad { get; set; }
        public List<string> IngredientesRemovidos { get; set; } = new();
        public string? NotasEspeciales { get; set; }
        public decimal AhorroInterno { get; set; }
        public decimal Subtotal { get; set; }
    }

    public class PedidoPersonalizadoRequest
    {
        public string TipoServicio { get; set; } = "";
        public string? Observaciones { get; set; }
    }
}