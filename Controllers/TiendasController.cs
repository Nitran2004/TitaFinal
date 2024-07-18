using Microsoft.AspNetCore.Mvc;

namespace ProyectoIdentity.Controllers
{
    public class TiendasController : Controller
    {
        public IActionResult Create()
        {
            return View();
        }

        public IActionResult Index()
        {
            return View();
        }
    }
}
