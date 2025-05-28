using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ProyectoIdentity.Datos;
using ProyectoIdentity.Models;
using System.Linq;
using System.Threading.Tasks;

namespace ProyectoIdentity.Controllers
{
    [Authorize]
    public class UsuariosController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _contexto;

        public UsuariosController(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager, ApplicationDbContext contexto)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _contexto = contexto;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var usuarios = await _contexto.AppUsuario.ToListAsync();
            var rolesUsuario = await _contexto.UserRoles.ToListAsync();
            var roles = await _contexto.Roles.ToListAsync();

            foreach (var usuario in usuarios)
            {
                var rolUsuario = rolesUsuario.FirstOrDefault(u => u.UserId == usuario.Id);
                if (rolUsuario == null)
                {
                    usuario.Rol = "Ninguno";
                }
                else
                {
                    var rol = roles.FirstOrDefault(r => r.Id == rolUsuario.RoleId);
                    usuario.Rol = rol?.Name ?? "Ninguno";
                }
            }

            return View(usuarios);
        }

        // Editar Usuario (Asignación de rol)
        [HttpGet]
        public IActionResult Editar(string id)
        {
            var usuarioBD = _contexto.AppUsuario.FirstOrDefault(u => u.Id == id);
            if (usuarioBD == null)
            {
                return NotFound();
            }

            var rolUsuario = _contexto.UserRoles.FirstOrDefault(u => u.UserId == usuarioBD.Id);
            var roles = _contexto.Roles.ToList();

            if (rolUsuario != null)
            {
                usuarioBD.IdRol = roles.FirstOrDefault(u => u.Id == rolUsuario.RoleId)?.Id;
            }

            usuarioBD.ListaRoles = roles.Select(u => new SelectListItem
            {
                Text = u.Name,
                Value = u.Id
            }).ToList();

            return View(usuarioBD);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(AppUsuario usuario)
        {
            if (ModelState.IsValid)
            {
                var usuarioBD = _contexto.AppUsuario.FirstOrDefault(u => u.Id == usuario.Id);
                if (usuarioBD == null)
                {
                    return NotFound();
                }

                var rolUsuario = _contexto.UserRoles.FirstOrDefault(u => u.UserId == usuarioBD.Id);
                if (rolUsuario != null)
                {
                    var rolActual = _contexto.Roles.Where(u => u.Id == rolUsuario.RoleId).Select(e => e.Name).FirstOrDefault();
                    await _userManager.RemoveFromRoleAsync(usuarioBD, rolActual);
                }

                await _userManager.AddToRoleAsync(usuarioBD, _contexto.Roles.FirstOrDefault(u => u.Id == usuario.IdRol).Name);
                _contexto.SaveChanges();
                return RedirectToAction(nameof(Index));
            }

            usuario.ListaRoles = _contexto.Roles.Select(u => new SelectListItem
            {
                Text = u.Name,
                Value = u.Id
            }).ToList();

            return View(usuario);
        }

        // Método para bloquear/desbloquear usuario
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult BloquearDesbloquear(string idUsuario)
        {
            var usuarioBD = _contexto.AppUsuario.FirstOrDefault(u => u.Id == idUsuario);

            if (usuarioBD == null)
            {
                return NotFound();
            }

            if (usuarioBD.LockoutEnd != null && usuarioBD.LockoutEnd > DateTime.Now)
            {
                usuarioBD.LockoutEnd = DateTime.Now;
            }
            else
            {
                usuarioBD.LockoutEnd = DateTime.Now.AddYears(100);
            }

            _contexto.SaveChanges();
            return RedirectToAction(nameof(Index));
        }

        // Método para borrar usuario
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Borrar(string idUsuario)
        {
            var usuarioBD = _contexto.AppUsuario.FirstOrDefault(u => u.Id == idUsuario);

            if (usuarioBD == null)
            {
                return NotFound();
            }

            _contexto.AppUsuario.Remove(usuarioBD);
            _contexto.SaveChanges();

            return RedirectToAction(nameof(Index));
        }

        // Editar perfil
        [HttpGet]
        public IActionResult EditarPerfil(string id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var usuarioBD = _contexto.AppUsuario.Find(id);
            if (usuarioBD == null)
            {
                return NotFound();
            }
            return View(usuarioBD);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarPerfil(AppUsuario appUsuario)
        {
            if (ModelState.IsValid)
            {
                var usuario = await _contexto.AppUsuario.FindAsync(appUsuario.Id);
                usuario.Nombre = appUsuario.Nombre;
                usuario.Url = appUsuario.Url;
                usuario.CodigoPais = appUsuario.CodigoPais;
                usuario.Telefono = appUsuario.Telefono;
                usuario.Ciudad = appUsuario.Ciudad;
                usuario.Pais = appUsuario.Pais;
                usuario.Direccion = appUsuario.Direccion;
                usuario.FechaNacimiento = appUsuario.FechaNacimiento;

                await _userManager.UpdateAsync(usuario);

                return RedirectToAction(nameof(Index), "Home");
            }

            return View(appUsuario);
        }

        // Cambiar contraseña
        [HttpGet]
        public IActionResult CambiarPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarPassword(CambiarPasswordViewModel cpViewModel, string email)
        {
            if (ModelState.IsValid)
            {
                var usuario = await _userManager.FindByEmailAsync(email);
                if (usuario == null)
                {
                    return RedirectToAction("Error");
                }

                var token = await _userManager.GeneratePasswordResetTokenAsync(usuario);
                var resultado = await _userManager.ResetPasswordAsync(usuario, token, cpViewModel.Password);
                if (resultado.Succeeded)
                {
                    return RedirectToAction("ConfirmacionCambioPassword");
                }
                else
                {
                    return View(cpViewModel);
                }
            }

            return View();
        }

        [HttpGet]
        public IActionResult ConfirmacionCambiarPassword()
        {
            return View();
        }

        // Acción para mostrar la vista de registro de administrador
        [HttpGet]
        public async Task<IActionResult> RegistroAdministrador()
        {
            var roles = await _contexto.Roles.ToListAsync();
            //var usuarios = await _contexto.AppUsuario.ToListAsync();
            // Check if Password property is populated for each user

            var model = new RegistroViewModel
            {
                ListaRoles = roles.Select(r => new SelectListItem
                {
                    Text = r.Name,
                    Value = r.Id
                }).ToList()
            };

            return View(model);
        }

        // Acción para procesar el formulario de registro de administrador
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistroAdministrador(RegistroViewModel model)
        {
            if (ModelState.IsValid)
            {
                var usuario = new IdentityUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    PhoneNumber = model.Telefono,
                };

                var resultado = await _userManager.CreateAsync(usuario, model.Password);

                if (resultado.Succeeded)
                {
                    var rol = await _roleManager.FindByIdAsync(model.IdRol);
                    if (rol != null)
                    {
                        await _userManager.AddToRoleAsync(usuario, rol.Name);
                    }
                    return RedirectToAction("Index", "Home");
                }

                foreach (var error in resultado.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            var roles = await _contexto.Roles.ToListAsync();
            model.ListaRoles = roles.Select(r => new SelectListItem
            {
                Text = r.Name,
                Value = r.Id
            }).ToList();

            return View(model);
        }


        public class PlainTextPasswordHasher : IPasswordHasher<IdentityUser>
        {
            public string HashPassword(IdentityUser user, string password)
            {
                // En lugar de hacer hashing, devolver la contraseña en texto claro
                return password;
            }

            public PasswordVerificationResult VerifyHashedPassword(IdentityUser user, string hashedPassword, string providedPassword)
            {
                // Verifica si la contraseña coincide
                return hashedPassword == providedPassword ? PasswordVerificationResult.Success : PasswordVerificationResult.Failed;
            }
        }



    }
}