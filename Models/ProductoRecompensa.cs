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
        public string? Descripcion { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        public virtual ICollection<HistorialCanje> HistorialCanjes { get; set; } = new List<HistorialCanje>();

        // ✅ Solo UNA propiedad de navegación
        public virtual Producto? Producto { get; set; }


        [NotMapped]
        public decimal RatioPuntosPrecio => PrecioOriginal > 0 ? Math.Round((decimal)PuntosNecesarios / PrecioOriginal, 2) : 0;

        [NotMapped]
        public string EstadoRatio
        {
            get
            {
                var ratio = RatioPuntosPrecio;
                if (ratio < 120) return "Bajo";
                if (ratio > 180) return "Alto";
                return "Óptimo";
            }
        }

        [NotMapped]
        public string ColorRatio
        {
            get
            {
                var ratio = RatioPuntosPrecio;
                if (ratio < 120) return "warning";
                if (ratio > 180) return "danger";
                return "success";
            }
        }
    }
}