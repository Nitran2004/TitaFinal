using Microsoft.AspNetCore.Mvc;

namespace ProyectoIdentity.Controllers
{
    public class MapasController : Controller
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
                return RedirectToAction("Create", "Fin");
            }
            if (accion == "Página siguiente")
            {

                return RedirectToAction("Create", "Montaña");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Index(string accion)
        {
            if (accion == "Página anterior")
            {

                return RedirectToAction("Create", "Fin");
            }
            if (accion == "Página siguiente")
            {

                return RedirectToAction("Create", "Rusas");
            }
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }
    }
}
