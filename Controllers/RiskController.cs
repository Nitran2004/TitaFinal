using Microsoft.AspNetCore.Mvc;

namespace ProyectoIdentity.Controllers
{
    public class RiskController : Controller
    {
        public IActionResult Create()
        {
            return View();
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Tratamiento()
        {
            return View();
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(string accion)
        {
            if (accion == "Página siguiente")
            {

                return RedirectToAction("Index", "Threat");
            }
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }
    }
}
