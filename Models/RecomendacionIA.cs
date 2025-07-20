using System.ComponentModel.DataAnnotations;

namespace ProyectoIdentity.Models
{
    // Modelo para recomendaciones de IA
    public class RecomendacionIA
    {
        public int ProductoId { get; set; } = -1;
        public string Respuesta { get; set; } = string.Empty;
        public double Puntuacion { get; set; } = 0;
        public string NombreProducto { get; set; } = string.Empty;
        public string Categoria { get; set; } = string.Empty;
        public decimal Precio { get; set; } = 0;
        public string? Descripcion { get; set; }
        public string? Alergenos { get; set; }
        public string? Ingredientes { get; set; }
        public string? InfoNutricional { get; set; }
        public int Cantidad { get; set; } = 0;
    }

    // Modelo para solicitudes de chat
    public class SolicitudChat
    {
        public int Id { get; set; }
        public string? UsuarioId { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public string EstadoSolicitud { get; set; } = string.Empty;
        public string? RespuestaIA { get; set; }
        public int? ProductoRecomendadoId { get; set; }
        public Producto? ProductoRecomendado { get; set; } // ✅ AGREGADA: Propiedad de navegación
        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        public DateTime? FechaActualizacion { get; set; }
    }

    // Modelo para historial de chat
    public class HistorialChat
    {
        public int Id { get; set; }
        public string? UsuarioId { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public string Respuesta { get; set; } = string.Empty;
        public string TipoMensaje { get; set; } = string.Empty;
        public int? ProductoId { get; set; }
        public Producto? Producto { get; set; }
        public DateTime Fecha { get; set; } = DateTime.Now;
    }

    // Modelos para las APIs
    public class ChatRequest
    {
        [Required]
        public string Mensaje { get; set; } = string.Empty;
    }

    public class RespuestaChat
    {
        public bool Exito { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public RecomendacionIA? Recomendacion { get; set; }
        public List<Producto>? ProductosAlternativos { get; set; }
        public string TipoRespuesta { get; set; } = string.Empty;
    }

    public class SugerenciasRequest
    {
        public string? Categoria { get; set; }
    }

    public class RespuestaSugerencias
    {
        public List<string> Sugerencias { get; set; } = new();
        public string Categoria { get; set; } = string.Empty;
    }

    public class MetricasChat
    {
        public int TotalConsultas { get; set; }
        public int RecomendacionesExitosas { get; set; }
        public int ConsultasSinResultado { get; set; }
        public double TasaExito { get; set; }
        public List<string> CategoriasPopulares { get; set; } = new();
        public List<string> ConsultasRecientes { get; set; } = new();
    }

    public class ConsultaRequest
    {
        [Required]
        public string Consulta { get; set; } = string.Empty;
    }
}