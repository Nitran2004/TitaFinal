using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace ProyectoIdentity.Models
{
    // Atributo personalizado para validar dominios de email
    public class ValidarDominioEmailAttribute : ValidationAttribute
    {
        // Lista de dominios permitidos
        private readonly string[] _dominiosPermitidos =
        {
    "hotmail.com", "gmail.com", "outlook.com", "yahoo.com",
    "udla.edu.ec", "mit.edu", "espol.edu.ec", "uce.edu.ec",
    "uide.edu.ec", "ups.edu.ec", "utpl.edu.ec", "ucuenca.edu.ec",
    "unl.edu.ec", "usfq.edu.ec", "ug.edu.ec", "uazuay.edu.ec",
    "puce.edu.ec", "unach.edu.ec"
};

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value != null)
            {
                var email = value.ToString();
                var dominio = email.Split('@').LastOrDefault();

                // Validar si el dominio está en la lista de dominios permitidos
                if (_dominiosPermitidos.Contains(dominio))
                {
                    return ValidationResult.Success; // El dominio es válido
                }
                else
                {
                    return new ValidationResult("El dominio del correo electrónico no está permitido.");
                }
            }

            return new ValidationResult("El email es obligatorio.");
        }
    }

    public class UsuarioViewModel
    {
        [Required(ErrorMessage = "El email es obligatorio")]
        [EmailAddress(ErrorMessage = "El formato del email es inválido")]
        [ValidarDominioEmail]
        public string Email { get; set; }

        // Otras propiedades del modelo
    }
}
