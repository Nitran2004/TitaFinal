using Microsoft.AspNetCore.Mvc;
using System.Media;
using Microsoft.AspNetCore.Hosting;
using System.IO;

namespace ProyectoIdentity.Controllers
{
    public class AssetController : Controller
    {
        private readonly IWebHostEnvironment _env;
        private SoundPlayer player;
        private string[] canciones;
        private int posicion = 0;

        public AssetController(IWebHostEnvironment env)
        {
            _env = env;
            // Construir las rutas completas a los archivos de canciones en wwwroot
            canciones = new string[]
            {
                Path.Combine(_env.WebRootPath, "Canciones", "Ejemplo3.wav")
            };
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Create(string accion)
        {
            if (accion == "parar")
            {
                player?.Stop();
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
                return RedirectToAction("Create", "Threat");
            }
            return View();
        }
    }
}
