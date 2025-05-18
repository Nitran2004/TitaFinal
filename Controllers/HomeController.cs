using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoIdentity.Datos;
using ProyectoIdentity.Models;
using System.Diagnostics;

namespace ProyectoIdentity.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger,
                              ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        // GET: /Home/Index
        public IActionResult Index()
        {
            // Ya no necesitamos cargar productos en el Index
            // porque ahora solo mostramos enlaces a las categorías
            return View();
        }

        //[Authorize(Roles= "Registrado,Administrador")]
        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        //public async Task<IActionResult> Index()
        //{
        //    // Obtener todos los productos de la base de datos
        //    var productos = await _context.Productos.ToListAsync();
        //    return View(productos);
        //}

        //[HttpPost]
        //public async Task<IActionResult> AgregarAlCarrito(int productoId, int cantidad)
        //{
        //    var producto = await _context.Productos.FindAsync(productoId);
        //    if (producto == null) return NotFound();

        //    // Redireccionar a la página de resumen del pedido
        //    return RedirectToAction("VerPedidoTemporal", "Pedidos");
        //}


    }
}
