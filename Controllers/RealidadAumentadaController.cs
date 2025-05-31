using Microsoft.AspNetCore.Mvc;
using ProyectoIdentity.Datos;
using System.Threading.Tasks;
using System.IO; // Agregar esta línea para Path.Combine

namespace ProyectoIdentity.Controllers
{
    [Route("[controller]")] // Agregar esta atribución de ruta
    public class RealidadAumentadaController : Controller
    {
        private readonly ApplicationDbContext _context;

        public RealidadAumentadaController(ApplicationDbContext context) => _context = context;

        [HttpGet("VistaAR")] // Especificar la ruta
        public async Task<IActionResult> VistaAR(int? id)
        {
            if (id == null) return NotFound();

            var producto = await _context.Productos.FindAsync(id);
            if (producto == null) return NotFound();

            ViewBag.ProductoId = id;
            ViewBag.ProductoNombre = producto.Nombre;
            ViewBag.ProductoPrecio = producto.Precio;
            // Determinar qué modelo 3D debe usarse según el nombre del producto
            ViewBag.ModeloPath = DeterminarModelo3D(producto.Nombre);

            return View();
        }

        [HttpGet("VistaSimple")]
        public IActionResult VistaSimple()
        {
            return View();
        }

        // En tu RealidadAumentadaController.cs
        [HttpGet("GetGLBFile")]
        public IActionResult GetGLBFile()
        {
            var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "models3d", "pizza1.glb");

            // Agrega logs para depuración
            Console.WriteLine($"Ruta del archivo: {path}");
            Console.WriteLine($"El archivo existe: {System.IO.File.Exists(path)}");

            if (!System.IO.File.Exists(path))
            {
                // Devolver una respuesta más detallada
                return NotFound($"Archivo no encontrado en: {path}");
            }

            // Usar ContentType específico para GLTF Binary
            return PhysicalFile(path, "model/gltf-binary");
        }

        // También podemos agregar un método simple de prueba
        [HttpGet("Test")]
        public IActionResult Test()
        {
            return Content("El controlador RealidadAumentada está funcionando correctamente");
        }

        private string DeterminarModelo3D(string nombreProducto)
        {
            if (string.IsNullOrEmpty(nombreProducto)) return "";

            // Normalizar el nombre para comparación
            string nombreNormalizado = nombreProducto.ToLower().Trim();

            // Switch para asignar modelo según el nombre del producto
            switch (nombreNormalizado)
            {
                case "pepperoni":
                    return "/models3d/pizza1.glb"; // Asegúrate de que esta ruta sea correcta
                case "mi champ":
                    return "/models3d/pizza2.glb"; // Asegúrate de que esta ruta sea correcta
                case "say cheese":
                    return "/models3d/pizza3.glb"; // Asegúrate de que esta ruta sea correcta
                case "verace":
                    return "/models3d/pizza4.glb"; // Asegúrate de que esta ruta sea correcta
                default:
                    return ""; // Cadena vacía indica usar el cubo rojo por defecto
            }
        }
    }
}