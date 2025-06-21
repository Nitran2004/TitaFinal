using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProyectoIdentity.Models
{
    public class Valoracion
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int PedidoId { get; set; }

        [Required]
        public string UsuarioId { get; set; } = "";

        [Range(1, 5)]
        public int ValoracionGeneral { get; set; }

        [Range(0, 5)]
        public int ValoracionCalidad { get; set; }

        [Range(0, 5)]
        public int ValoracionTiempo { get; set; }

        [MaxLength(1000)]
        public string? Comentarios { get; set; }

        [Required]
        public DateTime Fecha { get; set; }

        // Propiedades de navegación
        [ForeignKey("PedidoId")]
        public virtual Pedido? Pedido { get; set; }

        [ForeignKey("UsuarioId")]
        public virtual AppUsuario? Usuario { get; set; }
    }
}