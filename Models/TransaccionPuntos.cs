using System.ComponentModel.DataAnnotations;

namespace ProyectoIdentity.Models
{
    // Modelo para el historial de transacciones de puntos
    public class TransaccionPuntos
    {
        public int Id { get; set; }

        [Required]
        public string UsuarioId { get; set; } = string.Empty;

        [Required]
        public int Puntos { get; set; }

        [Required]
        [StringLength(20)]
        public string Tipo { get; set; } = string.Empty;

        [StringLength(200)]
        public string? Descripcion { get; set; }

        public DateTime Fecha { get; set; } = DateTime.Now;

        public int? PedidoId { get; set; }

        public int? RecompensaId { get; set; }
    }
}
