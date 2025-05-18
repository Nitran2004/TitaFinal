using Microsoft.AspNetCore.Mvc;
using ProyectoIdentity.Datos;
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

        // GET: RealidadAumentada/VistaAR/5
        public async Task<IActionResult> VistaAR(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Obtener el producto (aunque por ahora mostraremos siempre el mismo cubo)
            var producto = await _context.Productos.FindAsync(id);
            if (producto == null)
            {
                return NotFound();
            }

            // Pasar el producto a la vista para uso futuro
            ViewBag.ProductoId = id;
            ViewBag.ProductoNombre = producto.Nombre;

            return View();
        }

        // Acción para cargar modelos 3D (para implementación futura)
        public IActionResult GetModel3D(int id)
        {
            // Por ahora retorna siempre el mismo modelo de cubo
            // En el futuro, podría retornar diferentes modelos según el producto
            var modelPath = "/models/red-cube.glb";

            return Json(new { modelPath = modelPath });
        }
    }
}