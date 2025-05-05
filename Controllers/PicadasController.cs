using Microsoft.AspNetCore.Mvc;

namespace ProyectoIdentity.Controllers
{
    public class PicadasController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Picadas()
        {
            return View();
        }
        public IActionResult BreadSticks()
        {
            return View();
        }

        public IActionResult BreadSticksVerace()
        {
            return View();
        }

        public IActionResult NachosCheddar()
        {
            return View();
        }

        public IActionResult NachosVerace()
        {
            return View();
        }
    }
}
