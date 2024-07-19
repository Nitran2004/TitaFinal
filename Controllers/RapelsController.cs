using Microsoft.AspNetCore.Mvc;

namespace ProyectoIdentity.Controllers
{
    
    public class RapelsController : Controller
    {
        public IActionResult Create()
        {
            return View();
        }

        // GET: Rapels/Create
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

                return RedirectToAction("Create", "Gps");
            }
            return View();
        }
        public IActionResult Privacy()
        {
            return View();
        }
    }
}
