// HistorialCanje.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace ProyectoIdentity.Models
{
    public class HistorialCanje
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UsuarioId { get; set; }

        public int ProductoRecompensaId { get; set; }

        public int PuntosCanjeados { get; set; }

        public DateTime FechaCanje { get; set; }

        [ForeignKey("UsuarioId")]
        public virtual IdentityUser Usuario { get; set; }

        [ForeignKey("ProductoRecompensaId")]
        public virtual ProductoRecompensa ProductoRecompensa { get; set; }
    }
}