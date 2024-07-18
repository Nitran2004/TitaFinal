using Microsoft.AspNetCore.Mvc;

namespace ProyectoIdentity.Controllers
{
    public class BomasController : Controller
    {
        // GET: Bomas/Create
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

                return RedirectToAction("Index", "Bomas");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Index(string accion)
        {
            if (accion == "Página siguiente")
            {

                return RedirectToAction("Create", "Vueltas");
            }
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }
    }
}
