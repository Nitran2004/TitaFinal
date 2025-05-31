using System.ComponentModel.DataAnnotations;

namespace ProyectoIdentity.Models
{
    public class CuponCanjeado
    {
        public int Id { get; set; }

        [Required]
        public int CuponId { get; set; }
        public virtual Cupon Cupon { get; set; }

        public string UsuarioId { get; set; } // Puede ser null para usuarios anónimos
        public virtual AppUsuario Usuario { get; set; }

        [Required]
        public string CodigoQR { get; set; }

        public DateTime FechaCanje { get; set; }

        public decimal TotalOriginal { get; set; }

        public decimal DescuentoAplicado { get; set; }

        public decimal TotalConDescuento { get; set; }

        public string ProductosCanjeados { get; set; } // JSON con los productos

        public int? PedidoId { get; set; } // Relación con el pedido generado
        public virtual Pedido Pedido { get; set; }
    }
}