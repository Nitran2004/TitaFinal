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

        // Navegación
        public virtual ProductoRecompensa? ProductoRecompensa { get; set; }
    }
}