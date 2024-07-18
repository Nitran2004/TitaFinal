using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ProyectoIdentity.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class BooksController : Controller
    {
        [AllowAnonymous] // Permitir acceso público a la lista de alineamientos

        public IActionResult Index()
        {
            return View();
        }

        [AllowAnonymous] // Permitir acceso público a la lista de alineamientos

        public IActionResult Create()
        {
            return View();
        }


    }
}
