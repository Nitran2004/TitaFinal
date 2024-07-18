using Microsoft.AspNetCore.Mvc;

namespace ProyectoIdentity.Controllers
{
    public class AssetController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Create(string accion)
        {
            if (accion == "Página siguiente")
            {

                return RedirectToAction("Create", "Threat");
            }
            return View();
        }
    }
}
