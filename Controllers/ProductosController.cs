using Microsoft.AspNetCore.Mvc;
using ProyectoIdentity.Datos;
using Microsoft.EntityFrameworkCore;
using ProyectoIdentity.Models;

namespace Proyecto1_MZ_MJ.Controllers
{
    public class ProductosController : Controller
    {

        private readonly ApplicationDbContext _context;


        public ProductosController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var productos = await _context.Productos.ToListAsync();
            return View(productos);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Producto producto, IFormFile? imageFile)
        {
            if (ModelState.IsValid)
            {
                if (imageFile != null && imageFile.Length > 0)
                {
                    using var memoryStream = new MemoryStream();
                    await imageFile.CopyToAsync(memoryStream);
                    producto.Imagen = memoryStream.ToArray();
                }

                _context.Add(producto);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(producto);
        }

        public async Task<IActionResult> Menu()
        {
            var productos = await _context.Productos.ToListAsync();
            return View(productos);
        }

        public async Task<IActionResult> Pizzas()
        {
            var productos = await _context.Productos.ToListAsync();
            return View(productos);
        }

        public async Task<IActionResult> Sanduches()
        {
            var productos = await _context.Productos.ToListAsync();
            return View(productos);
        }

        public async Task<IActionResult> Picadas()
        {
            var productos = await _context.Productos.ToListAsync();
            return View(productos);
        }

        public async Task<IActionResult> Bebidas()
        {
            var productos = await _context.Productos.ToListAsync();
            return View(productos);
        }

        public async Task<IActionResult> Promos()
        {
            var productos = await _context.Productos.ToListAsync();
            return View(productos);
        }

        public async Task<IActionResult> Cerveza()
        {
            var productos = await _context.Productos.ToListAsync();
            return View(productos);
        }

        public async Task<IActionResult> Cocteles()
        {
            var productos = await _context.Productos.ToListAsync();
            return View(productos);
        }

        public async Task<IActionResult> Shots()
        {
            var productos = await _context.Productos.ToListAsync();
            return View(productos);
        }

        //public async Task<IActionResult> Detalle(int id)
        //{
        //    var producto = await _context.Productos.FindAsync(id);
        //    if (producto == null) return NotFound();
        //    return View(producto);  // Buscará Views/Productos/Detalle.cshtml
        //}

        // ProductosController.cs
        public async Task<IActionResult> Detalle(int id)
        {
            // Find the product by its ID
            var producto = await _context.Productos.FindAsync(id);

            if (producto == null)
            {
                return NotFound();
            }

            return View(producto);
        }



    }
}
