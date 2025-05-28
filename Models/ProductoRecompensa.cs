using ProyectoIdentity.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProyectoIdentity.Models
{
    public class ProductoRecompensa
    {
        public int Id { get; set; }
        public int? ProductoId { get; set; }

        [Required]
        [StringLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [Required]
        public decimal PrecioOriginal { get; set; }

        [Required]
        public int PuntosNecesarios { get; set; }

        public byte[] Imagen { get; set; }

        [StringLength(50)]
        public string? Categoria { get; set; }

        // ✅ Solo UNA propiedad de navegación
        public virtual Producto? Producto { get; set; }
    }
}