// Models/Cupon.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProyectoIdentity.Models
{
    public class Cupon
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Descripcion { get; set; }

        [Required]
        [StringLength(50)]
        public string TipoPromocion { get; set; } = string.Empty;

        [StringLength(200)]
        public string? DiasAplicables { get; set; }

        [StringLength(500)]
        public string? ProductosAplicables { get; set; }

        [StringLength(200)]
        public string? CategoriasAplicables { get; set; }

        public decimal? DescuentoPorcentaje { get; set; }
        public decimal? DescuentoFijo { get; set; }

        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }

        public bool Activo { get; set; }

        [Required]
        [StringLength(50)]
        public string CodigoQR { get; set; } = string.Empty;

        public DateTime FechaCreacion { get; set; }

        // CAMBIAR DE byte[] A string para URLs
        [StringLength(300)]
        public byte[]? ImagenCupon { get; set; }
        // Propiedad calculada - no se mapea en la BD
        [NotMapped]
        public bool EsValidoHoy
        {
            get
            {
                if (!Activo || DateTime.Now.Date < FechaInicio.Date || DateTime.Now.Date > FechaFin.Date)
                    return false;

                if (string.IsNullOrEmpty(DiasAplicables))
                    return true;

                var diaDeLaSemana = DateTime.Now.ToString("dddd", new System.Globalization.CultureInfo("es-ES"));
                var diasPermitidos = DiasAplicables.Split(',');

                // Mapear días en inglés a español
                var mapaDias = new Dictionary<string, string>
                {
                    {"Monday", "Lunes"}, {"Tuesday", "Martes"}, {"Wednesday", "Miercoles"},
                    {"Thursday", "Jueves"}, {"Friday", "Viernes"}, {"Saturday", "Sabado"}, {"Sunday", "Domingo"}
                };

                var diaActual = DateTime.Now.DayOfWeek.ToString();
                if (mapaDias.ContainsKey(diaActual))
                {
                    var diaEspanol = mapaDias[diaActual];
                    return diasPermitidos.Any(d => d.Trim().Equals(diaEspanol, StringComparison.OrdinalIgnoreCase));
                }

                return false;
            }
        }
    }
}