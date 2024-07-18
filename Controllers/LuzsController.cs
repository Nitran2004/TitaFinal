using Microsoft.AspNetCore.Mvc;

namespace ProyectoIdentity.Controllers
{
    public class LuzsController : Controller
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

                return RedirectToAction("Create", "Investigador");
            }
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }
    }
}
