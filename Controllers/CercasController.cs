using Microsoft.AspNetCore.Mvc;

namespace ProyectoIdentity.Controllers
{
    public class CercasController : Controller
    {
        // GET: Cercas/Create
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

                return RedirectToAction("Index", "Cercas");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Index(string accion)
        {
            if (accion == "Página siguiente")
            {

                return RedirectToAction("Create", "Cajas");
            }
            return View();
        }
        public IActionResult Privacy()
        {
            return View();
        }
    }
}
