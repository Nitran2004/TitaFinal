using Microsoft.AspNetCore.Mvc;

namespace ProyectoIdentity.Controllers
{
    public class Sobre2Controller : Controller
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

                return RedirectToAction("Index", "Sobre3");
            }
            return View();
        }
    }
}
