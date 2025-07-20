using System.ComponentModel.DataAnnotations;

namespace ProyectoIdentity.Models
{
    // Tabla de tipos de alérgenos disponibles
    public class TipoAlergeno
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Descripcion { get; set; }

        public bool Activo { get; set; } = true;

        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        // Relación con los alérgenos de usuarios
        public virtual ICollection<UsuarioAlergeno> UsuarioAlergenos { get; set; } = new List<UsuarioAlergeno>();
    }

    // Tabla de relación entre usuarios y sus alérgenos
    public class UsuarioAlergeno
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(450)] // Tamaño estándar para UserId de Identity
        public string UsuarioId { get; set; } = string.Empty;

        [Required]
        public int TipoAlergenoId { get; set; }

        public DateTime FechaRegistro { get; set; } = DateTime.Now;

        [MaxLength(500)]
        public string? Notas { get; set; } // Notas adicionales del usuario sobre su alergia

        // Propiedades de navegación
        public virtual TipoAlergeno TipoAlergeno { get; set; } = null!;
    }

    // ViewModel para la vista de gestión de alérgenos
    public class GestionAlergenosViewModel
    {
        public string UsuarioId { get; set; } = string.Empty;
        public string NombreUsuario { get; set; } = string.Empty;
        public List<TipoAlergeno> TiposAlergenosDisponibles { get; set; } = new();
        public List<UsuarioAlergeno> AlergenosUsuario { get; set; } = new();
        public List<int> AlergenosSeleccionados { get; set; } = new();
    }

    // ViewModel para recomendaciones basadas en alérgenos
    public class RecomendacionAlergenosViewModel
    {
        public string NombreUsuario { get; set; } = string.Empty;
        public List<string> AlergenosUsuario { get; set; } = new();
        public List<Producto> ProductosRecomendados { get; set; } = new();
        public List<Producto> ProductosNoRecomendados { get; set; } = new();
        public int TotalProductosAnalizados { get; set; }
        public string MensajeRecomendacion { get; set; } = string.Empty;
    }

    // DTO para actualizar alérgenos del usuario
    public class ActualizarAlergenosDto
    {
        public List<int> TiposAlergenosIds { get; set; } = new();
        public string? NotasGenerales { get; set; }
    }
}