using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoIdentity.Datos;
using ProyectoIdentity.Models;
using System.Threading.Tasks;

namespace ProyectoIdentity.Controllers
{
    public class RealidadAumentadaController : Controller
    {
        private readonly ApplicationDbContext _context;

        public RealidadAumentadaController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> VistaAR(int id)
        {
            // Obtener el producto por ID
            var producto = await _context.Productos.FindAsync(id);

            if (producto == null)
            {
                return NotFound();
            }

            // Crear ViewModel para la vista AR
            var viewModel = new ProductoARViewModel
            {
                Id = producto.Id,
                Nombre = producto.Nombre,
                Descripcion = producto.Descripcion,
                Precio = producto.Precio,
                Categoria = producto.Categoria,
                ImagenBase64 = Convert.ToBase64String(producto.Imagen)
            };

            return View(viewModel);
        }

        // Método adicional para obtener información del modelo 3D si lo necesitas
        [HttpGet]
        public async Task<IActionResult> ObtenerModelo3D(int id)
        {
            var producto = await _context.Productos.FindAsync(id);

            if (producto == null)
            {
                return NotFound();
            }

            // Aquí podrías devolver información específica del modelo 3D
            // Por ejemplo, URL del archivo .gltf o .glb si lo tienes almacenado
            var modelInfo = new
            {
                id = producto.Id,
                nombre = producto.Nombre,
                categoria = producto.Categoria,
                // Si tienes modelos 3D almacenados, podrías devolver la URL aquí
                modeloUrl = GetModelo3DUrl(producto.Categoria, producto.Nombre)
            };

            return Json(modelInfo);
        }

        // Método helper para obtener la URL del modelo 3D según la categoría
        private string GetModelo3DUrl(string categoria, string nombre)
        {
            // Mapear categorías a modelos 3D predefinidos
            // En producción, querrías tener modelos específicos para cada producto
            switch (categoria?.ToLower())
            {
                case "pizza":
                    return "/models/pizza.glb";
                case "bebidas":
                    return "/models/bebida.glb";
                case "sanduches":
                    return "/models/sandwich.glb";
                default:
                    return "/models/default.glb";
            }
        }
    }

    // ViewModel para la vista AR
    public class ProductoARViewModel
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public decimal? Precio { get; set; }
        public string Categoria { get; set; }
        public string ImagenBase64 { get; set; }
    }
}