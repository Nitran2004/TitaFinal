using Microsoft.AspNetCore.Mvc;

namespace ProyectoIdentity.Controllers
{
    public class Sobre5Controller : Controller
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

                return RedirectToAction("Create", "Sobre6");
            }
            return View();
        }
    }
}
