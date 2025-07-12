using System.ComponentModel.DataAnnotations;

namespace ProyectoIdentity.Models
{
    public class Recompensa
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Descripcion { get; set; }

        [Required]
        public int PuntosRequeridos { get; set; }

        public bool Activa { get; set; } = true;

        // Relación opcional con producto (si la recompensa es un producto específico)
        public int? ProductoId { get; set; }
        public virtual Producto? Producto { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.Now;
    }
}
