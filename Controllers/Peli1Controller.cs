using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Media;

namespace ProyectoIdentity.Controllers
{
    [Authorize(Roles = "Administrador,Lector 15 libros")]
    public class Peli1Controller : Controller
    {
        private static SoundPlayer player;
        private static string[] canciones = { "Canciones/Ejemplo12.wav" }; // Actualiza con la ruta correcta a tus archivos de audio
        private static int posicion = 0;
        private static bool isPlaying = false;

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
            else if (accion == "anterior" && posicion > 0)
            {
                posicion--;
                player = new SoundPlayer(canciones[posicion]);
                player.LoadAsync();
                player.PlaySync();
                isPlaying = true;
            }
            else if (accion == "siguiente" && posicion < canciones.Length - 1)
            {
                posicion++;
                player = new SoundPlayer(canciones[posicion]);
                player.LoadAsync();
                player.PlaySync();
                isPlaying = true;
            }
            else if (accion == "Página siguiente")
            {
                return RedirectToAction("Create", "Peli2");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Index(string accion)
        {
            if (accion == "Página siguiente")
            {
                return RedirectToAction("Create", "Peli2");
            }
            return View();
        }
    }
}
