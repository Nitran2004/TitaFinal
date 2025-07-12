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

        // Actualizar SOLO este método en PedidoRecomendacionController

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

                // ✅ OBTENER SUCURSAL DESDE TEMPDATA SI EXISTE, SINO LA PRIMERA
                int? sucursalId = TempData["SucursalId"] as int?;
                var sucursal = sucursalId.HasValue
                    ? await _context.Sucursales.FindAsync(sucursalId.Value)
                    : await _context.Sucursales.FirstOrDefaultAsync();

                if (sucursal == null)
                {
                    return Json(new { success = false, message = "No hay sucursales disponibles" });
                }

                // ✅ CREAR PEDIDO
                var pedido = new ProyectoIdentity.Models.Pedido
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

                // ✅ CREAR DETALLES
                foreach (var item in request.Items)
                {
                    var detalle = new ProyectoIdentity.Models.PedidoDetalle
                    {
                        PedidoId = pedido.Id,
                        ProductoId = item.Id,
                        Cantidad = item.Cantidad,
                        PrecioUnitario = item.Precio,
                        IngredientesRemovidos = "[]",
                        NotasEspeciales = "Pedido por recomendación IA"
                    };

                    _context.PedidoDetalles.Add(detalle);
                }

                await _context.SaveChangesAsync();

                // ✅ NUEVO: AGREGAR PUNTOS DE FIDELIDAD
                await AgregarPuntosPorPedidoRecomendacion(usuario.Id, pedido.Total);

                // ✅ MANTENER INFO DE SUCURSAL PARA CONFIRMACIÓN
                TempData.Keep("SucursalSeleccionada");
                TempData.Keep("DireccionSeleccionada");
                TempData.Keep("SucursalId");

                return Json(new
                {
                    success = true,
                    pedidoId = pedido.Id,
                    message = "Pedido creado exitosamente",
                    puntosGanados = (int)(pedido.Total * 30) // ✅ DEVOLVER PUNTOS GANADOS
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

        // ✅ NUEVO MÉTODO - Agregar al final del controlador
        private async Task<bool> AgregarPuntosPorPedidoRecomendacion(string usuarioId, decimal totalPedido)
        {
            try
            {
                if (string.IsNullOrEmpty(usuarioId)) return false;

                var usuario = await _context.AppUsuario.FindAsync(usuarioId);
                if (usuario == null) return false;

                // Calcular puntos ganados (30 puntos por dólar)
                int puntosGanados = (int)(totalPedido * 30);

                // Agregar puntos al usuario
                usuario.PuntosFidelidad = (usuario.PuntosFidelidad ?? 0) + puntosGanados;

                // Crear registro de transacción de puntos
                var transaccion = new ProyectoIdentity.Models.TransaccionPuntos
                {
                    UsuarioId = usuarioId,
                    Puntos = puntosGanados,
                    Tipo = "Ganancia",
                    Descripcion = $"Puntos ganados por pedido de recomendación IA - Total: ${totalPedido:F2}",
                    Fecha = DateTime.Now
                };

                _context.TransaccionesPuntos.Add(transaccion);
                await _context.SaveChangesAsync();

                Console.WriteLine($"[PUNTOS] Usuario {usuarioId} ganó {puntosGanados} puntos por pedido de ${totalPedido}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Error al agregar puntos: {ex.Message}");
                return false;
            }
        }

        // ACTUALIZAR el método Confirmacion en PedidoRecomendacionController

        // Ver confirmación del pedido - DESDE BASE DE DATOS
        public async Task<IActionResult> Confirmacion(int id)
        {
            var pedido = await _context.Pedidos
                .Include(p => p.Detalles)
                .ThenInclude(d => d.Producto)
                .Include(p => p.Sucursal) // ✅ INCLUIR SUCURSAL
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pedido == null)
            {
                return NotFound();
            }

            // ✅ SI NO HAY DATOS EN TEMPDATA, USAR LA SUCURSAL DEL PEDIDO
            if (TempData["SucursalSeleccionada"] == null && pedido.Sucursal != null)
            {
                TempData["SucursalSeleccionada"] = pedido.Sucursal.Nombre;

                // Buscar el CollectionPoint para obtener la dirección
                var collectionPoint = await _context.CollectionPoints
                    .Include(cp => cp.Sucursal)
                    .FirstOrDefaultAsync(cp => cp.Sucursal.Id == pedido.SucursalId);

                if (collectionPoint != null)
                {
                    TempData["DireccionSeleccionada"] = collectionPoint.Address;
                }
            }

            // ✅ MANTENER DATOS DE TEMPDATA PARA LA VISTA
            TempData.Keep("SucursalSeleccionada");
            TempData.Keep("DireccionSeleccionada");
            TempData.Keep("SucursalId");

            return View(pedido);
        }

        // Listar pedidos del usuario logueado - DESDE BASE DE DATOS

        [Authorize(Roles = "Administrador,Registrado")]
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