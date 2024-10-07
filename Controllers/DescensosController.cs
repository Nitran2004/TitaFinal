using Microsoft.AspNetCore.Mvc;

namespace ProyectoIdentity.Controllers
{
    public class DescensosController : Controller
    {

        // GET: Descensos/Create
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

                return RedirectToAction("Index", "Home");
            }
            return View();
        }
    }
}
