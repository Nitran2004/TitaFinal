using Microsoft.AspNetCore.Mvc;

namespace ProyectoIdentity.Controllers
{
    public class MilitarsController : Controller
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

                return RedirectToAction("Create", "Gps");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Index(string accion)
        {
            if (accion == "Página siguiente")
            {

                return RedirectToAction("Index", "Fogatas");
            }
            return View();
        }
        public IActionResult Privacy()
        {
            return View();
        }
    }
}
