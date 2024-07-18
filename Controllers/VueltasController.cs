using Microsoft.AspNetCore.Mvc;

namespace ProyectoIdentity.Controllers
{
    public class VueltasController : Controller
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
        public IActionResult Index(string accion)
        {
            if (accion == "Página siguiente")
            {

                return RedirectToAction("Create", "Descensos");
            }
            return View();
        }
    }
}
