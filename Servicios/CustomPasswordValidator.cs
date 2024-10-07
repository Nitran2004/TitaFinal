using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ProyectoIdentity.Servicios
{
    public class CustomPasswordValidator : PasswordValidator<IdentityUser>
    {
        public override async Task<IdentityResult> ValidateAsync(UserManager<IdentityUser> manager, IdentityUser user, string password)
        {
            // Llamar a la validación base de Identity
            var result = await base.ValidateAsync(manager, user, password);

            var errors = new List<IdentityError>();

            // Verifica si la contraseña contiene un número
            if (!password.Any(char.IsDigit))
            {
                errors.Add(new IdentityError
                {
                    Code = "PasswordRequiresDigit",
                    Description = "La contraseña debe contener al menos un número."
                });
            }

            // Verifica si la contraseña contiene un carácter no alfanumérico
            if (!password.Any(ch => !char.IsLetterOrDigit(ch)))
            {
                errors.Add(new IdentityError
                {
                    Code = "PasswordRequiresNonAlphanumeric",
                    Description = "La contraseña debe contener al menos un carácter no alfanumérico."
                });
            }

            // Verifica si la contraseña contiene al menos una letra mayúscula
            if (!password.Any(char.IsUpper))
            {
                errors.Add(new IdentityError
                {
                    Code = "PasswordRequiresUpper",
                    Description = "La contraseña debe contener al menos una letra mayúscula."
                });
            }

            // Verifica si la contraseña contiene al menos una letra minúscula
            if (!password.Any(char.IsLower))
            {
                errors.Add(new IdentityError
                {
                    Code = "PasswordRequiresLower",
                    Description = "La contraseña debe contener al menos una letra minúscula."
                });
            }

            // Si hay errores personalizados, los combinamos con los errores originales
            if (errors.Any())
            {
                return IdentityResult.Failed(errors.ToArray());
            }

            return result; // Si no hay errores personalizados, retorna el resultado base.
        }
    }
}
