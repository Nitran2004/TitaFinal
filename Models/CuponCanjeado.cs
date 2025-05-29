// Models/CuponCanjeado.cs
using System.ComponentModel.DataAnnotations;

namespace ProyectoIdentity.Models
{
    public class CuponCanjeado
    {
        public int Id { get; set; }

        public int CuponId { get; set; }
        public virtual Cupon Cupon { get; set; }

        public string UsuarioId { get; set; }
        public virtual AppUsuario Usuario { get; set; }

        public string ClienteId { get; set; }
        public virtual AppUsuario Cliente { get; set; }

        [Required]
        [StringLength(50)]
        public string CodigoQR { get; set; }

        public DateTime FechaCanje { get; set; }

        public decimal DescuentoAplicado { get; set; }
        public decimal TotalOriginal { get; set; }
        public decimal TotalConDescuento { get; set; }

        [StringLength(50)]
        public string EstadoCanje { get; set; }

        public string ProductosIncluidos { get; set; }

        public string DetallesCanje { get; set; }
    }
}