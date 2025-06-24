// ACTUALIZAR ProductoViewModel en Models/PersonalizacionSimple.cs
// ✅ AGREGAR LA PROPIEDAD QUE FALTABA

using System.ComponentModel.DataAnnotations;

namespace ProyectoIdentity.Models
{
    // ViewModel para el CRUD de productos
    public class ProductoViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres")]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "La descripción no puede exceder 500 caracteres")]
        public string? Descripcion { get; set; }

        [Required(ErrorMessage = "La categoría es obligatoria")]
        [StringLength(50, ErrorMessage = "La categoría no puede exceder 50 caracteres")]
        public string Categoria { get; set; } = string.Empty;

        [Required(ErrorMessage = "El precio es obligatorio")]
        [Range(0.01, 9999.99, ErrorMessage = "El precio debe estar entre $0.01 y $9999.99")]
        public decimal Precio { get; set; }

        [StringLength(1000, ErrorMessage = "La información nutricional no puede exceder 1000 caracteres")]
        public string? InfoNutricional { get; set; }

        [StringLength(500, ErrorMessage = "Los alérgenos no pueden exceder 500 caracteres")]
        public string? Alergenos { get; set; }

        // ✅ ESTAS DOS PROPIEDADES SON NECESARIAS:

        // Para la versión simple (campo de texto)
        [Display(Name = "Ingredientes Removibles")]
        public string? IngredientesTexto { get; set; }

        // Para la versión completa (JSON con Nombre, Costo, Removible)
        public string? IngredientesJson { get; set; }

        // Para mostrar imagen existente
        public byte[]? ImagenExistente { get; set; }
    }

    // Request para eliminar producto
    public class EliminarProductoRequest
    {
        public int Id { get; set; }
    }

    // Modelo para ingredientes (ya existe pero lo incluyo para referencia)
    public class IngredienteProducto
    {
        public string Nombre { get; set; } = string.Empty;
        public decimal Costo { get; set; }
        public bool Removible { get; set; } = true;
    }
}