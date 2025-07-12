using System.ComponentModel.DataAnnotations;

namespace ProyectoIdentity.Models
{
    public class AdminRecompensaViewModel
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public decimal PrecioOriginal { get; set; }
        public int PuntosNecesarios { get; set; }
        public string Categoria { get; set; } = string.Empty;
        public byte[]? Imagen { get; set; }
        public int? ProductoId { get; set; }
        public Producto? Producto { get; set; }

        // Propiedades calculadas
        public decimal RatioPuntosPrecio => PrecioOriginal > 0 ? PuntosNecesarios / PrecioOriginal : 0;
        public string EstadoRatio => RatioPuntosPrecio <= 150 ? "Excelente" :
                                   RatioPuntosPrecio <= 200 ? "Moderado" : "Alto";
    }
}