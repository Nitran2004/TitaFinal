using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ProyectoIdentity.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class Sobre9Controller : Controller
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
        public IActionResult Index(string accion)
        {
            if (accion == "Página siguiente")
            {

                return RedirectToAction("Create", "Sobre1");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(string accion)
        {
            if (accion == "Página siguiente")
            {

                return RedirectToAction("Create", "Sobre1");
            }
            return View();
        }
    }
}
