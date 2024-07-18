using Microsoft.AspNetCore.Mvc;

namespace ProyectoIdentity.Controllers
{
    public class DietasController : Controller
    {
        public IActionResult Create()
        {
            return View();
        }

        public IActionResult Index()
        {
            return View();
        }
        public IActionResult Tratamiento(string code)
        {
            // Aquí puedes realizar cualquier lógica adicional antes de mostrar la vista "Tratamiento.cshtml"
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(string accion)
        {
            if (accion == "Página siguiente")
            {

                return RedirectToAction("Create", "Montaña");
            }
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }
    }
}
