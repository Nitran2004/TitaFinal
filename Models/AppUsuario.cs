using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProyectoIdentity.Models
{
    public class AppUsuario : IdentityUser
    {
        public string Nombre { get; set; }
        [DisplayName("Usuario")]
        public string Url { get; set; }
        public int CodigoPais { get; set; }
        public string Telefono { get; set; }
        public string Pais { get; set; }
        public string Ciudad { get; set; }
        public string Direccion { get; set; }
        public DateTime FechaNacimiento { get; set; }  // Corrección aquí
        public bool Estado { get; set; }

        // Propiedad para almacenar la contraseña en texto plano 
        //[NotMapped]
        public string Password { get; set; }

        //Nuevas propiedades para usar roles y asugnacion de un rol a un usuario 

        [NotMapped]
        [Display(Name = "Rol para el usuario")]
        public string IdRol { get; set; }
        [NotMapped]
        public string Rol { get; set; }
        [NotMapped]
        public IEnumerable<SelectListItem> ListaRoles { get; set; }

    }
}
