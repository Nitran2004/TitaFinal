// HistorialCanje.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace ProyectoIdentity.Models
{
    public class HistorialCanje
    {
        public int Id { get; set; }

        [Required]
        public string UsuarioId { get; set; } = string.Empty;

        public int ProductoRecompensaId { get; set; }

        public int PuntosUtilizados { get; set; }

        public DateTime FechaCanje { get; set; } = DateTime.Now;

        [StringLength(50)]
        public string? TipoServicio { get; set; }

        // En Models/HistorialCanje.cs - agregar estas propiedades
        public string Estado { get; set; } = "Preparándose";
        public bool ComentarioEnviado { get; set; } = false;
        public int? Calificacion { get; set; }
        public string? Comentario { get; set; }

        // Navegación
        public virtual ProductoRecompensa? ProductoRecompensa { get; set; }
    }
}