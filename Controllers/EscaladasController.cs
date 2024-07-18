using Microsoft.AspNetCore.Mvc;

namespace ProyectoIdentity.Controllers
{
    public class EscaladasController : Controller
    {
        // GET: Escaladas/Create
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

                return RedirectToAction("Index", "Escaladas");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Index(string accion)
        {
            if (accion == "Página siguiente")
            {

                return RedirectToAction("Create", "Bosques");
            }
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }
    }
}
