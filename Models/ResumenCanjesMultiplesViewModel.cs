// Agregar esta clase en tu carpeta Models

using ProyectoIdentity.Models;

namespace ProyectoIdentity.Models
{
    public class ResumenCanjesMultiplesViewModel
    {
        public AppUsuario Usuario { get; set; }
        public List<HistorialCanje> CanjesRealizados { get; set; } = new List<HistorialCanje>();
        public int TotalPuntosUtilizados { get; set; }
        public int CantidadRecompensas { get; set; }
        public int PuntosRestantes { get; set; }
        public DateTime FechaCanje { get; set; }
        public string CodigoCanje { get; set; }

        // Propiedades calculadas para la vista
        public decimal ValorTotalAhorrado { get; set; }
        // ✅ Mejora: Agregada validación de null safety
        public string CategoriasCanjeadas => string.Join(", ",
            CanjesRealizados?.Select(c => c.ProductoRecompensa?.Categoria)
                            ?.Where(cat => !string.IsNullOrEmpty(cat))
                            ?.Distinct() ?? Enumerable.Empty<string>());
    }
}