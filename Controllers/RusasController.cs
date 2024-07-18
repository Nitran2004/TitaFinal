using Microsoft.AspNetCore.Mvc;

namespace ProyectoIdentity.Controllers
{
    public class RusasController : Controller
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
            if (accion == "Página anterior")
            {

                return RedirectToAction("Index", "Mapas");
            }
            if (accion == "Página siguiente")
            {

                return RedirectToAction("Index", "Rusas");
            }
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }
    }
}
