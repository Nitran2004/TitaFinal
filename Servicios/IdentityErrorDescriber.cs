using Microsoft.AspNetCore.Identity;

namespace ProyectoIdentity.Servicios
{
    public class CustomIdentityErrorDescriber : IdentityErrorDescriber
    {
        public override IdentityError DuplicateUserName(string userName)
        {
            return new IdentityError
            {
                Code = nameof(DuplicateUserName),
                Description = $"El correo electrónico '{userName}' ya está en uso."
            };
        }

        public override IdentityError DuplicateEmail(string email)
        {
            return new IdentityError
            {
                Code = nameof(DuplicateEmail),
                Description = $"El correo electrónico '{email}' ya está en uso."
            };
        }

        // Puedes sobrescribir otros mensajes de error aquí
    }
}
