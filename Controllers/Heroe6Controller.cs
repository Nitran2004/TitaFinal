using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ProyectoIdentity.Controllers
{
    [Authorize(Roles = "Administrador")]

    public class Heroe6Controller : Controller
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

                return RedirectToAction("Index", "Heroe6");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Index(string accion)
        {
            if (accion == "Página siguiente")
            {

                return RedirectToAction("Create", "Heroe7");
            }
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }
    }
}
