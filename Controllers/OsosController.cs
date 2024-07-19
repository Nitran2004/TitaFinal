using Microsoft.AspNetCore.Mvc;

namespace ProyectoIdentity.Controllers
{
    
    public class OsosController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        // GET: Osos/Create
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(string accion)
        {
            if (accion == "Página siguiente")
            {

                return RedirectToAction("Index", "Osos");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Index(string accion)
        {
            if (accion == "Página siguiente")
            {

                return RedirectToAction("Create", "Pistolas");
            }
            return View();
        }
        public IActionResult Privacy()
        {
            return View();
        }
    }
}
