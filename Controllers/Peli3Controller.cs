using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ProyectoIdentity.Controllers
{
    [Authorize(Roles = "Administrador")]

    public class Peli3Controller : Controller
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
            if (accion == "Página siguiente")
            {

                return RedirectToAction("Index", "Peli3");
            }
            return View();
        }
    }
}
