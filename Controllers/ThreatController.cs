using Microsoft.AspNetCore.Mvc;

namespace ProyectoIdentity.Controllers
{
    public class ThreatController : Controller
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
        public IActionResult Create(string accion)
        {
            if (accion == "Página anterior")
            {

                return RedirectToAction("Create", "Asset");
            }
            if (accion == "Página siguiente")
            {

                return RedirectToAction("Create", "Control");
            }
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }
    }
}
