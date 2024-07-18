using Microsoft.AspNetCore.Mvc;

namespace ProyectoIdentity.Controllers
{
    public class FusiblesController : Controller
    {
        // GET: Fusibles/Create
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
