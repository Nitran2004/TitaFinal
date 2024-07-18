using Microsoft.AspNetCore.Mvc;

namespace ProyectoIdentity.Controllers
{
    public class CodigosController : Controller
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

                return RedirectToAction("Create", "Videos");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Index(string accion)
        {
            if (accion == "Página siguiente")
            {

                return RedirectToAction("Create", "Codigos");
            }
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }
    }
}
