using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace ProyectoIdentity.Models
{
    public class RegistroViewModel : IdentityUser
    {
        [Required(ErrorMessage = "El email es obligatorio")]
        [EmailAddress(ErrorMessage = "El formato del email es inválido")]
        [ValidarDominioEmail(ErrorMessage = "El dominio del correo no está permitido")]
        public string Email { get; set; }


        [Required(ErrorMessage = "La contraseña es obligatoria")]
        [StringLength(50, ErrorMessage = "La contraseña debe tener al menos {2} caracteres y un máximo de {1} caracteres", MinimumLength = 5)]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña")]
        public string Password { get; set; }

        [Required(ErrorMessage = "La confirmación de la contraseña es obligatoria")]
        [Compare("Password", ErrorMessage = "La contraseña y la confirmación de la contraseña no coinciden")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirmar Contraseña")]
        public string ConfirmPassword { get; set; }

        // Otras propiedades
        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(20, ErrorMessage = "El nombre no puede tener más de 20 caracteres")]
        [RegularExpression(@"^([A-ZÁÉÍÓÚÑ][a-záéíóúñ]*(\s[A-ZÁÉÍÓÚÑ][a-záéíóúñ]*)*)*$", ErrorMessage = "El nombre debe empezar con mayúscula y solo puede contener letras")]
        public string Nombre { get; set; }
        [Required(ErrorMessage = "El nombre de usuario es obligatorio")]
        [StringLength(20, ErrorMessage = "El nombre de usuario no puede tener más de 20 caracteres")]
        [RegularExpression(@"^[A-Z][a-z]*[0-9]*$", ErrorMessage = "El nombre de usuario debe comenzar con una mayúscula seguido de letras minúsculas y puede terminar con números")]
        [Display(Name = "Usuario")]
        public string Url { get; set; }
        [Display(Name = "Codigo de país")]
        public int CodigoPais { get; set; }
        [Required(ErrorMessage = "El teléfono es obligatorio")]
        [RegularExpression(@"^\+?\d{10}$", ErrorMessage = "El formato del teléfono es inválido. Debe tener 10 dígitos.")]
        public string Telefono { get; set; }
        public string Pais { get; set; }
        public string Ciudad { get; set; }
        public string Direccion { get; set; }
        [Display(Name = "Fecha de Nacimiento")]
        public DateTime FechaNacimiento { get; set; }
        public bool Estado { get; set; }
        public string IdRol { get; set; }


        //Para seleccion de roles
        [Display(Name ="Seleccionar rol")]
        public string RolSeleccionado { get; set; }

        [Display(Name ="Rol Seleccionado")]
        public List<SelectListItem> ListaRoles { get; set; }
    }
}
