namespace ProyectoIdentity.Models
{
    public class RecomendacionProducto
    {
        public int ProductoId { get; set; }
        public string Respuesta { get; set; } = string.Empty;
        public double Puntuacion { get; set; }
        public string NombreProducto { get; set; } = string.Empty;
        public string Categoria { get; set; } = string.Empty;
        public decimal Precio { get; set; }

        // Campos adicionales de la tabla Productos
        public string? Descripcion { get; set; }
        public int Cantidad { get; set; }
        public string? InfoNutricional { get; set; }
        public string? Alergenos { get; set; }
        public string? Ingredientes { get; set; }
        public string? Imagen { get; set; }
    }
}