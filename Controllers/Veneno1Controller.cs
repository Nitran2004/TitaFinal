using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Media;

namespace ProyectoIdentity.Controllers
{
    [Authorize(Roles = "Administrador,Lector 15 libros")]
    public class Veneno1Controller : Controller
    {
        private static readonly string[] canciones = { "Canciones/Ejemplo13.wav" };
        private static SoundPlayer player = new SoundPlayer();
        private static int posicion = 0;
        private static bool isPlaying = false;

        // Método Create: No redirige automáticamente
        public IActionResult Create()
        {
            return View();
        }

        // Método Index: Carga la vista Index
        public IActionResult Index()
        {
            return View();
        }

        // Método POST para Create: Se asegura de que redirige correctamente a Index
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(string accion)
        {
            if (accion == "playPause")
            {
                if (isPlaying)
                {
                    player.Stop();
                    isPlaying = false;
                }
                else
                {
                    player = new SoundPlayer(canciones[posicion]);
                    player.LoadAsync();
                    player.PlaySync();
                    isPlaying = true;
                }
            }

            // Redirige al método Index del controlador Veneno1
            return RedirectToAction("Index", "Veneno1");
        }

        // Método POST para Index: Este es el que podría estar causando el problema
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public IActionResult Index(string accion)
        //{
        //    if (accion == "Página siguiente")
        //    {
        //        // En lugar de redirigir a Veneno2/Create, redirige a Index de Veneno1
        //        return RedirectToAction("Index", "Veneno1");
        //    }
        //    return View();
        //}
    }
}
