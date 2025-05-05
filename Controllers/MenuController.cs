using Microsoft.AspNetCore.Mvc;

namespace ProyectoIdentity.Controllers
{
    public class MenuController : Controller
    {
        public IActionResult Menu()
        {
            return View();
        }

        public IActionResult Pizzas()
        {
            
            return View(); // Redirige a la vista de recomendación para Mi Champ
        }

        public IActionResult Sanduches()
        {

            return View(); 
        }

        public IActionResult Picadas()
        {

            return View();
        }

        public IActionResult Bebidas()
        {

            return View();
        }

        public IActionResult Promos()
        {

            return View();
        }

        public IActionResult Cervezas()
        {

            return View();
        }

        public IActionResult Cocteles()
        {

            return View();
        }

        public IActionResult Shots()
        {

            return View();
        }
    }
}
