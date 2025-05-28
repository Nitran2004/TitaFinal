// UsuarioPuntos.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace ProyectoIdentity.Models
{
    public class UsuarioPuntos
    {
        public int Id { get; set; }

        [Required]
        public string UsuarioId { get; set; } = string.Empty;

        public int PuntosActuales { get; set; } = 0;

        public int PuntosGanados { get; set; } = 0;

        public int PuntosGastados { get; set; } = 0;

        public DateTime UltimaActualizacion { get; set; } = DateTime.Now;
    }
}