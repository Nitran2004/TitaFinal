using Microsoft.AspNetCore.Mvc;
using ProyectoIdentity.Datos;
using System.Threading.Tasks;

namespace ProyectoIdentity.Controllers
{
    public class RealidadAumentadaController : Controller
    {
        private readonly ApplicationDbContext _context;

        public RealidadAumentadaController(ApplicationDbContext context) => _context = context;

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

        private string DeterminarModelo3D(string nombreProducto)
        {
            if (string.IsNullOrEmpty(nombreProducto)) return "";

            // Normalizar el nombre para comparación
            string nombreNormalizado = nombreProducto.ToLower().Trim();

            // Switch para asignar modelo según el nombre del producto
            switch (nombreNormalizado)
            {
                case "pepperoni":
                    return "/models3d/pizza1.glb";
                case "mi champ":
                    return "/models3d/pizza2.glb";
                default:
                    return ""; // Cadena vacía indica usar el cubo rojo por defecto
            }
        }
    }
}