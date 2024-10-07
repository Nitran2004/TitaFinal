using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ProyectoIdentity.Controllers
{
    [Authorize(Roles = "Administrador,Lector 15 libros")]
    public class Mito6Controller : Controller
    {
        public IActionResult Create()
        {
            return View();
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(string accion)
        {
            if (accion == "Repetir Cuento")
            {
                // Redirige a la acción Create del controlador Mito1
                return RedirectToAction("Create", "Mito1");
            }
            else if (accion == "Página siguiente")
            {
                // Redirige a la acción Index del controlador Mito5
                return RedirectToAction("Index", "Mito5");
            }
            return View();
        }
    }
}
