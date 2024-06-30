using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoIdentity.Datos;

namespace ProyectoIdentity.Controllers
{
    [Authorize(Roles ="Administrador")]
    public class RolesController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _contexto;

        public RolesController(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager, ApplicationDbContext contexto)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _contexto = contexto;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var roles =_contexto.Roles.ToList();
            return View(roles);
        }

        [HttpGet]
        public IActionResult Crear()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task <IActionResult> Crear(IdentityRole rol)
        {
            if (await _roleManager.RoleExistsAsync(rol.Name))
            {
                return RedirectToAction("Index"); // Redirige a la acción Index en el controlador Roles
            }
            //Se crea el rol
            await _roleManager.CreateAsync(new IdentityRole() {Name = rol.Name });
            return View();
        }

        [HttpGet]
        public IActionResult Editar(string id)
        {
            if (String.IsNullOrEmpty(id))
            {
                return View();
            }
            else
            {
                //Actualizar el rol
                var rolBD = _contexto.Roles.FirstOrDefault(r=> r.Id == id);
                return View(rolBD);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(IdentityRole rol)
        {
            if (await _roleManager.RoleExistsAsync(rol.Name))
            {
                return RedirectToAction(nameof(Index));
            }
            //Se crea el rol
            var rolBD = _contexto.Roles.FirstOrDefault(r => r.Id == rol.Id);
            if (rolBD ==null)
            {
                return Redirect(nameof(Index));
            }
            rolBD.Name = rol.Name;
            rolBD.NormalizedName = rol.Name.ToUpper();
            var resultado =await _roleManager.UpdateAsync(rolBD);
            return RedirectToAction(nameof(Index));




        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Borrar(string id)
        {

            var rolBD = _contexto.Roles.FirstOrDefault(r => r.Id == id);
            if (rolBD == null)
            {
                return RedirectToAction(nameof(Index));
            }

            var usuariosParaEsteRol = _contexto.UserRoles.Where(u => u.RoleId == id).Count();
            if (usuariosParaEsteRol > 0)
            {
                return RedirectToAction(nameof(Index));

            }
            await _roleManager.DeleteAsync(rolBD);
            return RedirectToAction(nameof(Index));




        }
    }
}
