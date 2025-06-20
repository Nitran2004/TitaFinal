using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoIdentity.Datos;
using ProyectoIdentity.Models;
using System.Security.Claims;
using System.Text.Json;

namespace ProyectoIdentity.Controllers
{
    [Authorize] // Requiere que el usuario esté logueado
    public class PedidoRecomendacionController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ApplicationDbContext _context;

        public PedidoRecomendacionController(UserManager<IdentityUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        // Modelos para el carrito (mantener compatibilidad)
        public class ItemCarrito
        {
            public int Id { get; set; }
            public string Nombre { get; set; }
            public decimal Precio { get; set; }
            public int Cantidad { get; set; }
            public decimal Subtotal { get; set; }
        }

        // Vista del carrito
        public IActionResult VerCarrito()
        {
            return View();
        }

        // Procesar pedido desde el carrito - GUARDANDO EN BASE DE DATOS
        [HttpPost]
        public async Task<IActionResult> ProcesarPedido([FromBody] PedidoRequest request)
        {
            try
            {
                // Obtener el usuario logueado
                var usuario = await _userManager.GetUserAsync(User);
                if (usuario == null)
                {
                    return Json(new { success = false, message = "Usuario no encontrado" });
                }

                // Obtener sucursal
                var sucursal = await _context.Sucursales.FirstOrDefaultAsync();
                if (sucursal == null)
                {
                    return Json(new { success = false, message = "No hay sucursales disponibles" });
                }

                // ✅ CREAR PEDIDO EN LA BASE DE DATOS
                var pedido = new ProyectoIdentity.Models.Pedido // ← Especificar namespace completo
                {
                    Fecha = DateTime.Now,
                    UsuarioId = usuario.Id,
                    Estado = "Preparándose",
                    Total = request.Items.Sum(i => i.Subtotal),
                    TipoServicio = request.TipoServicio ?? "Servir aquí",
                    SucursalId = sucursal.Id
                };

                _context.Pedidos.Add(pedido);
                await _context.SaveChangesAsync();

                // ✅ CREAR DETALLES DE PEDIDO
                foreach (var item in request.Items)
                {
                    var detalle = new ProyectoIdentity.Models.PedidoDetalle // ← Especificar namespace completo
                    {
                        PedidoId = pedido.Id,
                        ProductoId = item.Id,
                        Cantidad = item.Cantidad,
                        PrecioUnitario = item.Precio,
                        IngredientesRemovidos = "[]", // Sin personalización
                        NotasEspeciales = "Pedido por recomendación IA" // Usar NotasEspeciales del detalle
                    };

                    _context.PedidoDetalles.Add(detalle);
                }

                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    pedidoId = pedido.Id,
                    message = "Pedido creado exitosamente"
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Error al procesar el pedido: " + ex.Message
                });
            }
        }

        // Ver confirmación del pedido - DESDE BASE DE DATOS
        public async Task<IActionResult> Confirmacion(int id)
        {
            var pedido = await _context.Pedidos
                .Include(p => p.Detalles)
                .ThenInclude(d => d.Producto)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pedido == null)
            {
                return NotFound();
            }

            return View(pedido);
        }

        // Listar pedidos del usuario logueado - DESDE BASE DE DATOS
        public async Task<IActionResult> MisPedidos()
        {
            var usuario = await _userManager.GetUserAsync(User);
            if (usuario == null)
            {
                return RedirectToAction("Acceso", "Cuentas");
            }

            // ✅ OBTENER PEDIDOS DE RECOMENDACIÓN DESDE LA BD
            var pedidosUsuario = await _context.Pedidos
                .Include(p => p.Detalles)
                .ThenInclude(d => d.Producto)
                .Where(p => p.UsuarioId == usuario.Id &&
                           p.Detalles.Any(d => d.NotasEspeciales == "Pedido por recomendación IA"))
                .OrderByDescending(p => p.Fecha)
                .ToListAsync();

            return View(pedidosUsuario);
        }

        // Último pedido - DESDE BASE DE DATOS
        public async Task<IActionResult> UltimoPedido()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            Console.WriteLine($"[DEBUG] Buscando pedidos de recomendación para usuario: {userId}");

            var ultimoPedido = await _context.Pedidos
                .Include(p => p.Detalles)
                .Where(p => p.UsuarioId == userId &&
                           p.Detalles.Any(d => d.NotasEspeciales == "Pedido por recomendación IA"))
                .OrderByDescending(p => p.Fecha)
                .FirstOrDefaultAsync();

            Console.WriteLine($"[DEBUG] Pedido de recomendación encontrado: {ultimoPedido?.Id ?? 0}");

            if (ultimoPedido == null)
            {
                Console.WriteLine("[DEBUG] No hay pedidos de recomendación - redirigiendo");
                TempData["Mensaje"] = "No tienes pedidos de recomendación registrados";
                return RedirectToAction("Recomendacion", "MenuRecomendacion");
            }

            Console.WriteLine($"[DEBUG] Redirigiendo a Confirmacion con ID: {ultimoPedido.Id}");
            return RedirectToAction("Confirmacion", new { id = ultimoPedido.Id });
        }

        // Listar todos los pedidos (para administración) - DESDE BASE DE DATOS
        public async Task<IActionResult> ListarPedidos()
        {
            if (!User.IsInRole("Administrador"))
            {
                return Forbid();
            }

            var pedidos = await _context.Pedidos
                .Include(p => p.Detalles)
                .ThenInclude(d => d.Producto)
                .Where(p => p.Detalles.Any(d => d.NotasEspeciales == "Pedido por recomendación IA"))
                .OrderByDescending(p => p.Fecha)
                .ToListAsync();

            return View(pedidos);
        }

        // Modelo para recibir el pedido
        public class PedidoRequest
        {
            public List<ItemCarrito> Items { get; set; }
            public string TipoServicio { get; set; }
            public string Observaciones { get; set; }
        }
    }
}