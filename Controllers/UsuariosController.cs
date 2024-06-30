using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ProyectoIdentity.Datos;
using ProyectoIdentity.Models;

namespace ProyectoIdentity.Controllers
{
    [Authorize]
    public class UsuariosController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ApplicationDbContext _contexto;

        public UsuariosController( UserManager<IdentityUser> userManager, ApplicationDbContext contexto)
        {
            _userManager = userManager;
            _contexto = contexto;
        }

        public IActionResult Index()
        {
            return View();
        }

        //Editar perfil
        [HttpGet]
        public IActionResult EditarPerfil(string id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var usuarioBd = _contexto.AppUsuario.Find(id);
            if (usuarioBd ==null)
            {
                return NotFound();
            }
            return View(usuarioBd);
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


        //Cambiar contraseña
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
                
                var usuario= await _userManager.FindByEmailAsync(email);
                if (usuario == null)
                {
                    return RedirectToAction("Error");
                }

                var token = await _userManager.GeneratePasswordResetTokenAsync(usuario);
                var resultado =await _userManager.ResetPasswordAsync(usuario, token, cpViewModel.Password);
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


        //Cambiar contraseña
        [HttpGet]
        public IActionResult ConfirmacionCambiarPassword()
        {

            return View();
        }
    }
}
