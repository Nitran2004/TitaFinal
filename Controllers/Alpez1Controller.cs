using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Media;

namespace ProyectoIdentity.Controllers
{
    [Authorize(Roles = "Administrador,Lector 15 libros")]
    public class Alpez1Controller : Controller
    {
        private SoundPlayer player = new SoundPlayer();
        private string[] canciones = { "Canciones/Ejemplo10.wav" }; // Actualiza con la ruta correcta a tus archivos de audio
        private int posicion = 0;

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
            if (accion == "parar")
            {
                player.Stop();
            }
            else if (accion == "reanudar")
            {
                player = new SoundPlayer(canciones[posicion]);
                player.LoadAsync();
                player.PlaySync();
            }
            else if (accion == "anterior" && posicion > 0)
            {
                posicion--;
                player = new SoundPlayer(canciones[posicion]);
                player.LoadAsync();
                player.PlaySync();
            }
            else if (accion == "siguiente" && posicion < canciones.Length - 1)
            {
                posicion++;
                player = new SoundPlayer(canciones[posicion]);
                player.LoadAsync();
                player.PlaySync();
            }
            else if (accion == "Página siguiente")
            {
                return RedirectToAction("Create", "Alpez2");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Index(string accion)
        {
            if (accion == "Página siguiente")
            {
                return RedirectToAction("Create", "Alpez2");
            }
            return View();
        }
    }
}
