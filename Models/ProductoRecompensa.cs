// ProductoRecompensa.cs
using ProyectoIdentity.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProyectoIdentity.Models
{
    public class ProductoRecompensa
    {
        [Key]
        public int Id { get; set; }

        public int ProductoId { get; set; }

        [Required]
        [StringLength(100)]
        public string Nombre { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal PrecioOriginal { get; set; }

        public int PuntosNecesarios { get; set; }

        public string Imagen { get; set; }

        [Required]
        [StringLength(50)]
        public string Categoria { get; set; }

        [ForeignKey("ProductoId")]
        public virtual Producto Producto { get; set; }
    }
}