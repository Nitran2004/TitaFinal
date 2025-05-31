using System.ComponentModel.DataAnnotations;

namespace ProyectoIdentity.Models
{
    public class Cupon
    {
        public int Id { get; set; }

        [Required]
        public string Nombre { get; set; }

        public string Descripcion { get; set; }

        [Required]
        public string TipoDescuento { get; set; } // "Fijo", "Porcentaje", "3x2"

        public decimal ValorDescuento { get; set; } // Monto fijo o porcentaje

        public decimal MontoMinimo { get; set; } // Monto mínimo para aplicar

        public string ProductosAplicables { get; set; } // IDs de productos separados por coma

        public string DiasAplicables { get; set; } // Días de la semana separados por coma

        [Required]
        public string CodigoQR { get; set; } // Código único para el QR

        public DateTime FechaCreacion { get; set; }

        public DateTime? FechaExpiracion { get; set; }

        public bool Activo { get; set; } = true;

        public int LimiteUsos { get; set; } = 1; // Cuántas veces se puede usar

        public int VecesUsado { get; set; } = 0;
    }
}