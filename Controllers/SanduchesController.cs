using Microsoft.AspNetCore.Mvc;

namespace ProyectoIdentity.Controllers
{
    public class SanduchesController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Sanduches()
        {
            return View();
        }
        public IActionResult CarneMechada()
        {
            return View();
        }

        public IActionResult Tradicional()
        {
            return View();
        }

        public IActionResult Veggie()
        {
            return View();
        }
    }
}
