using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoIdentity.Datos;
using ProyectoIdentity.Models;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Mailjet.Client.Resources;

namespace ProyectoIdentity.Controllers
{
    [Authorize]

    public class PersonalizacionController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PersonalizacionController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var productos = await _context.Productos.ToListAsync();
            return View(productos);
        }

        // Detalle del producto con personalización
        public async Task<IActionResult> Detalle(int id)
        {
            var producto = await _context.Productos.FindAsync(id);
            if (producto == null) return NotFound();

            ViewBag.Ingredientes = GetIngredientesProducto(producto);
            return View(producto);
        }

        // Agregar al carrito personalizado
        [HttpPost]
        public async Task<IActionResult> AgregarAlCarrito([FromBody] PersonalizacionRequest request)
        {
            try
            {
                var producto = await _context.Productos.FindAsync(request.ProductoId);
                if (producto == null)
                    return Json(new { success = false, message = "Producto no encontrado" });

                // Calcular ahorro interno solo para administradores
                decimal ahorroInterno = 0;
                if (User.IsInRole("Administrador"))
                {
                    ahorroInterno = CalcularAhorro(producto, request.IngredientesRemovidos);
                }

                // Obtener carrito existente
                var carrito = GetCarrito();

                // Agregar producto al carrito
                var itemCarrito = new ItemCarritoPersonalizado
                {
                    Id = request.ProductoId,
                    Nombre = producto.Nombre,
                    Precio = producto.Precio,
                    Cantidad = request.Cantidad,
                    IngredientesRemovidos = request.IngredientesRemovidos,
                    NotasEspeciales = request.NotasEspeciales,
                    AhorroInterno = ahorroInterno,
                    Subtotal = producto.Precio * request.Cantidad
                };

                carrito.Add(itemCarrito);
                SetCarrito(carrito);

                // Mensaje personalizado según el rol
                string mensaje = User.IsInRole("Administrador")
                    ? $"{producto.Nombre} agregado al carrito. Ahorro interno: ${ahorroInterno:F2}"
                    : "¡Producto agregado al carrito exitosamente!";

                return Json(new
                {
                    success = true,
                    message = mensaje,
                    totalItems = carrito.Sum(c => c.Cantidad)
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al agregar al carrito" });
            }
        }

        // Ver carrito
        public IActionResult VerCarrito()
        {
            var carrito = GetCarrito();
            return View(carrito);
        }

        [HttpPost]
        public async Task<IActionResult> ProcesarPedido([FromBody] PedidoPersonalizadoRequest request)
        {
            try
            {
                var carrito = GetCarrito();
                if (!carrito.Any())
                    return Json(new { success = false, message = "El carrito está vacío" });

                // Obtener sucursal
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                Console.WriteLine($"[DEBUG] PersonalizacionController - Usuario ID: {userId}");

                var sucursal = await _context.Sucursales.FirstOrDefaultAsync();
                if (sucursal == null)
                {
                    // Crear sucursal por defecto si no existe
                    sucursal = new Sucursal
                    {
                        Nombre = "Verace Pizza",
                        Direccion = "Av. de los Shyris N35-52",
                        Latitud = -0.180653,
                        Longitud = -78.487834
                    };
                    _context.Sucursales.Add(sucursal);
                    await _context.SaveChangesAsync();
                }

                // Calcular total
                decimal totalPedido = carrito.Sum(c => c.Subtotal);

                // Crear pedido
                var pedido = new Pedido
                {
                    Fecha = DateTime.Now,
                    UsuarioId = userId,
                    Estado = "Preparándose",
                    Total = totalPedido,
                    TipoServicio = request.TipoServicio,
                    SucursalId = sucursal.Id
                };

                _context.Pedidos.Add(pedido);
                await _context.SaveChangesAsync();

                Console.WriteLine($"[DEBUG] PersonalizacionController - Pedido creado con ID: {pedido.Id}, Total: {totalPedido}");

                // Crear detalles CON personalización
                foreach (var item in carrito)
                {
                    var detalle = new PedidoDetalle
                    {
                        PedidoId = pedido.Id,
                        ProductoId = item.Id,
                        Cantidad = item.Cantidad,
                        PrecioUnitario = item.Precio,
                        IngredientesRemovidos = JsonSerializer.Serialize(item.IngredientesRemovidos),
                        NotasEspeciales = item.NotasEspeciales
                    };

                    _context.PedidoDetalles.Add(detalle);
                }

                await _context.SaveChangesAsync();

                // ✅ AGREGAR PUNTOS AL USUARIO
                Console.WriteLine($"[DEBUG] PersonalizacionController - Agregando puntos. Total: {totalPedido}");
                await AgregarPuntosAUsuario(userId, totalPedido);

                // Limpiar carrito
                HttpContext.Session.Remove("CarritoPersonalizado");

                return Json(new { success = true, pedidoId = pedido.Id });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] PersonalizacionController - Error en ProcesarPedido: {ex.Message}");
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        private async Task AgregarPuntosAUsuario(string usuarioId, decimal totalPedido)
        {
            Console.WriteLine($"[DEBUG] PersonalizacionController - AgregarPuntosAUsuario iniciado - UsuarioId: {usuarioId}, Total: {totalPedido}");

            if (string.IsNullOrEmpty(usuarioId))
            {
                Console.WriteLine("[DEBUG] UsuarioId es null o vacío - SALIENDO");
                return;
            }

            var usuario = await _context.AppUsuario.FindAsync(usuarioId);
            if (usuario == null)
            {
                Console.WriteLine($"[DEBUG] Usuario no encontrado con ID: {usuarioId} - SALIENDO");
                return;
            }

            Console.WriteLine($"[DEBUG] Usuario encontrado: {usuario.Email}, Puntos actuales: {usuario.PuntosFidelidad}");

            // Calcular puntos ganados (30 puntos por dólar)
            int puntosGanados = (int)(totalPedido * 30);
            Console.WriteLine($"[DEBUG] Puntos a agregar: {puntosGanados}");

            // Agregar puntos al usuario
            int puntosAnteriores = usuario.PuntosFidelidad ?? 0;
            usuario.PuntosFidelidad = puntosAnteriores + puntosGanados;

            Console.WriteLine($"[DEBUG] Puntos anteriores: {puntosAnteriores}, Nuevos puntos: {usuario.PuntosFidelidad}");

            try
            {
                // Crear registro de transacción de puntos
                var transaccion = new TransaccionPuntos
                {
                    UsuarioId = usuarioId,
                    Puntos = puntosGanados,
                    Tipo = "Ganancia",
                    Descripcion = $"Puntos ganados por pedido personalizado - Total: ${totalPedido:F2}",
                    Fecha = DateTime.Now
                };

                _context.TransaccionesPuntos.Add(transaccion);

                // Guardar cambios
                await _context.SaveChangesAsync();

                Console.WriteLine("[DEBUG] ✅ Cambios guardados exitosamente en la base de datos");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Error al guardar en la base de datos: {ex.Message}");
                Console.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");
            }
        }
        public async Task<IActionResult> Confirmacion(int id)
        {
            var pedido = await _context.Pedidos
                .Include(p => p.Detalles)
                .ThenInclude(d => d.Producto)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pedido == null) return NotFound();
            return View(pedido);
        }

        // Panel de administrador
        public async Task<IActionResult> AdminAnalisis()
        {
            var analisis = await GenerarAnalisisSimple();
            return View(analisis);
        }

        // MÉTODOS PRIVADOS
        private List<Ingrediente> GetIngredientesProducto(Producto producto)
        {
            if (string.IsNullOrEmpty(producto.Ingredientes)) return new();
            try
            {
                return JsonSerializer.Deserialize<List<Ingrediente>>(producto.Ingredientes) ?? new();
            }
            catch { return new(); }
        }

        private decimal CalcularAhorro(Producto producto, List<string> ingredientesRemovidos)
        {
            var ingredientes = GetIngredientesProducto(producto);
            return ingredientesRemovidos.Sum(nombreIngrediente =>
                ingredientes.FirstOrDefault(i => i.Nombre == nombreIngrediente && i.Removible)?.Costo ?? 0);
        }

        private List<ItemCarritoPersonalizado> GetCarrito()
        {
            var carritoJson = HttpContext.Session.GetString("CarritoPersonalizado");
            return string.IsNullOrEmpty(carritoJson) ? new() : JsonSerializer.Deserialize<List<ItemCarritoPersonalizado>>(carritoJson) ?? new();
        }

        private void SetCarrito(List<ItemCarritoPersonalizado> carrito)
        {
            HttpContext.Session.SetString("CarritoPersonalizado", JsonSerializer.Serialize(carrito));
        }

        private async Task<List<AnalisisSimple>> GenerarAnalisisSimple()
        {
            var resultados = new List<AnalisisSimple>();
            var fechaDesde = DateTime.Now.AddMonths(-1);
            var pedidos = await _context.Pedidos
                .Include(p => p.Detalles)
                .Where(p => p.Fecha >= fechaDesde)
                .ToListAsync();

            var productos = await _context.Productos.ToListAsync();

            foreach (var producto in productos.Where(p => !string.IsNullOrEmpty(p.Ingredientes)))
            {
                var ingredientes = GetIngredientesProducto(producto);
                foreach (var ingrediente in ingredientes.Where(i => i.Removible))
                {
                    int vecesRemovido = 0;
                    foreach (var pedido in pedidos)
                    {
                        foreach (var detalle in pedido.Detalles.Where(d => d.ProductoId == producto.Id))
                        {
                            if (!string.IsNullOrEmpty(detalle.IngredientesRemovidos))
                            {
                                try
                                {
                                    var removidos = JsonSerializer.Deserialize<List<string>>(detalle.IngredientesRemovidos) ?? new();
                                    if (removidos.Contains(ingrediente.Nombre))
                                        vecesRemovido += detalle.Cantidad;
                                }
                                catch { }
                            }
                        }
                    }

                    if (vecesRemovido > 0)
                    {
                        resultados.Add(new AnalisisSimple
                        {
                            NombreIngrediente = ingrediente.Nombre,
                            NombreProducto = producto.Nombre,
                            CostoUnitario = ingrediente.Costo,
                            VecesRemovido = vecesRemovido,
                            AhorroTotal = vecesRemovido * ingrediente.Costo
                        });
                    }
                }
            }
            return resultados.OrderByDescending(r => r.AhorroTotal).ToList();
        }

        public async Task<IActionResult> UltimoPedido()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            Console.WriteLine($"[DEBUG] Buscando pedidos para usuario: {userId}");

            var ultimoPedido = await _context.Pedidos
                .Where(p => p.UsuarioId == userId)
                .OrderByDescending(p => p.Fecha)
                .FirstOrDefaultAsync();

            Console.WriteLine($"[DEBUG] Pedido encontrado: {ultimoPedido?.Id ?? 0}");

            if (ultimoPedido == null)
            {
                Console.WriteLine("[DEBUG] No hay pedidos - redirigiendo a Index");
                TempData["Mensaje"] = "No tienes pedidos personalizados registrados";
                return RedirectToAction("Index");
            }

            Console.WriteLine($"[DEBUG] Redirigiendo a Confirmacion con ID: {ultimoPedido.Id}");
            return RedirectToAction("Confirmacion", new { id = ultimoPedido.Id });
        }

        [HttpPost]
        public IActionResult ActualizarCarrito([FromBody] List<ItemCarritoPersonalizado> carrito)
        {
            try
            {
                SetCarrito(carrito);
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
    }

    // MODELOS NECESARIOS
    public class PersonalizacionRequest
    {
        public int ProductoId { get; set; }
        public int Cantidad { get; set; } = 1;
        public List<string> IngredientesRemovidos { get; set; } = new();
        public string? NotasEspeciales { get; set; }
    }
    public class PedidoPersonalizadoRequest
    {
        public string TipoServicio { get; set; } = "";
        public string? Observaciones { get; set; }
    }
}