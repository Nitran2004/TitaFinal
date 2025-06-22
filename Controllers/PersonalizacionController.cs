using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoIdentity.Datos;
using ProyectoIdentity.Models;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Mailjet.Client.Resources;
using static ProyectoIdentity.Controllers.PedidoRecomendacionController;

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

        // ✅ MÉTODO ACTUALIZADO: Index con filtros de categoría
        public async Task<IActionResult> Index(string categoria)
        {
            var productosQuery = _context.Productos.AsQueryable();

            // Filtrar por categoría si se especifica
            if (!string.IsNullOrEmpty(categoria) && categoria.ToLower() != "todas")
            {
                productosQuery = productosQuery.Where(p => p.Categoria.ToLower() == categoria.ToLower());
            }

            var productos = await productosQuery.ToListAsync();

            // Obtener todas las categorías para el menú
            var categorias = await _context.Productos
                .Select(p => p.Categoria)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            // Pasar datos a la vista
            ViewBag.Categorias = categorias;
            ViewBag.CategoriaActual = string.IsNullOrEmpty(categoria) ? "todas" : categoria;

            return View(productos);
        }

        // ACTUALIZAR ESTE MÉTODO EN PersonalizacionController.cs

        public IActionResult IniciarPersonalizacion(int id)
        {
            // ✅ GUARDAR EL ID DEL PRODUCTO EN TEMPDATA PARA MANTENERLO EN EL FLUJO
            TempData["ProductoPersonalizacionId"] = id;

            // Redirigir a selección de punto de recolección indicando que es personalización
            return RedirectToAction("Seleccionar", "Recoleccion", new { esPersonalizacion = true });
        }

        // Detalle del producto con personalización
        public async Task<IActionResult> Detalle(int id)
        {
            // Verificar que tenga una sucursal seleccionada (viene de recolección)
            var sucursalSeleccionada = HttpContext.Session.GetInt32("SucursalSeleccionada");

            if (sucursalSeleccionada == null)
            {
                // Si no hay sucursal, redirigir a iniciar el flujo
                return RedirectToAction("IniciarPersonalizacion", new { id = id });
            }

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

        private List<ItemCarritoPersonalizado> ObtenerCarritoDeSession()
        {
            var carritoJson = HttpContext.Session.GetString("CarritoPersonalizado");
            if (string.IsNullOrEmpty(carritoJson))
            {
                return new List<ItemCarritoPersonalizado>();
            }

            try
            {
                return JsonSerializer.Deserialize<List<ItemCarritoPersonalizado>>(carritoJson)
                       ?? new List<ItemCarritoPersonalizado>();
            }
            catch
            {
                return new List<ItemCarritoPersonalizado>();
            }
        }

        [HttpPost]
        public async Task<IActionResult> ProcesarPedido([FromBody] PedidoRequest request)
        {
            try
            {
                var carritoItems = ObtenerCarritoDeSession();

                if (!carritoItems.Any())
                {
                    return Json(new { success = false, message = "El carrito está vacío" });
                }

                var total = carritoItems.Sum(item => item.Subtotal);
                var sucursalPorDefecto = _context.Sucursales.FirstOrDefault();

                if (sucursalPorDefecto == null)
                {
                    return Json(new { success = false, message = "No hay sucursales disponibles" });
                }

                // ✅ CORRECCIÓN: Asignar el UsuarioId correctamente
                var userId = User.Identity.IsAuthenticated ? User.FindFirstValue(ClaimTypes.NameIdentifier) : null;

                Console.WriteLine($"[DEBUG] ProcesarPedido - Usuario autenticado: {User.Identity.IsAuthenticated}");
                Console.WriteLine($"[DEBUG] ProcesarPedido - UsuarioId obtenido: {userId}");

                var pedido = new Pedido
                {
                    UsuarioId = userId, // ✅ CAMBIAR DE null A userId
                    SucursalId = sucursalPorDefecto.Id,
                    TipoServicio = request.TipoServicio,
                    Comentario = request.Observaciones,
                    Fecha = DateTime.Now,
                    Estado = "Preparándose",
                    Total = total,
                    Detalles = carritoItems.Select(item => new PedidoDetalle
                    {
                        ProductoId = item.Id,
                        PrecioUnitario = item.Precio,
                        Cantidad = item.Cantidad,
                        IngredientesRemovidos = item.IngredientesRemovidos.Any()
                            ? JsonSerializer.Serialize(item.IngredientesRemovidos)
                            : null,
                        NotasEspeciales = item.NotasEspeciales
                    }).ToList()
                };

                Console.WriteLine($"[DEBUG] ProcesarPedido - Pedido creado con UsuarioId: {pedido.UsuarioId}");

                _context.Pedidos.Add(pedido);
                await _context.SaveChangesAsync();

                Console.WriteLine("[DEBUG] ProcesarPedido - Pedido guardado en BD");

                // ✅ AGREGAR PUNTOS AL USUARIO SI ESTÁ AUTENTICADO
                if (User.Identity.IsAuthenticated && total > 0)
                {
                    Console.WriteLine($"[DEBUG] ProcesarPedido - Llamando AgregarPuntosAUsuario");
                    await AgregarPuntosAUsuario(userId, total);
                }

                LimpiarCarritoDeSession();

                return Json(new { success = true, pedidoId = pedido.Id });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] ProcesarPedido: {ex.Message}");
                return Json(new { success = false, message = ex.InnerException?.Message ?? ex.Message });
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

        public IActionResult Confirmacion(int id)
        {
            var pedido = _context.Pedidos
                .Include(p => p.Detalles)
                .ThenInclude(d => d.Producto) // Si necesitas el producto
                .FirstOrDefault(p => p.Id == id);

            if (pedido == null)
            {
                TempData["Error"] = "Pedido no encontrado";
                return RedirectToAction("Index");
            }

            return View(pedido);
        }

        // Panel de administrador
        public async Task<IActionResult> AdminAnalisis()
        {
            var analisis = await GenerarAnalisisSimple();
            return View(analisis);
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
                GuardarCarritoEnSession(carrito);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        private void GuardarCarritoEnSession(List<ItemCarritoPersonalizado> carrito)
        {
            var carritoJson = JsonSerializer.Serialize(carrito);
            HttpContext.Session.SetString("CarritoPersonalizado", carritoJson);
        }

        private void LimpiarCarritoDeSession()
        {
            HttpContext.Session.Remove("CarritoPersonalizado");
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
        [HttpPost]
        public async Task<IActionResult> GuardarValoracion([FromBody] ValoracionRequest request)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                // Verificar que el pedido pertenezca al usuario
                var pedido = await _context.Pedidos
                    .FirstOrDefaultAsync(p => p.Id == request.PedidoId && p.UsuarioId == userId);

                if (pedido == null)
                {
                    return Json(new { success = false, message = "Pedido no encontrado" });
                }

                // Verificar que no haya una valoración previa para este pedido
                var valoracionExistente = await _context.Valoraciones
                    .FirstOrDefaultAsync(v => v.PedidoId == request.PedidoId);

                if (valoracionExistente != null)
                {
                    return Json(new { success = false, message = "Este pedido ya ha sido valorado" });
                }

                // Crear nueva valoración
                var valoracion = new Valoracion
                {
                    PedidoId = request.PedidoId,
                    UsuarioId = userId,
                    ValoracionGeneral = request.ValoracionGeneral,
                    ValoracionCalidad = request.ValoracionCalidad,
                    ValoracionTiempo = request.ValoracionTiempo,
                    Comentarios = request.Comentarios,
                    Fecha = DateTime.Now
                };

                _context.Valoraciones.Add(valoracion);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Valoración guardada exitosamente" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Error al guardar valoración: {ex.Message}");
                return Json(new { success = false, message = "Error interno del servidor" });
            }
        }
        // AGREGAR SOLO ESTE MÉTODO SIMPLE en PersonalizacionController

        [HttpPost]
        public async Task<IActionResult> CambiarEstadoAEntregado([FromBody] dynamic request)
        {
            try
            {
                int pedidoId = request.PedidoId;
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                var pedido = await _context.Pedidos
                    .FirstOrDefaultAsync(p => p.Id == pedidoId && p.UsuarioId == userId);

                if (pedido == null)
                {
                    return Json(new { success = false, message = "Pedido no encontrado" });
                }

                if (pedido.Estado != "Listo para entregar")
                {
                    return Json(new { success = false, message = "El pedido no está listo para entregar" });
                }

                pedido.Estado = "Entregado";
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Estado actualizado a Entregado" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Error al cambiar estado: {ex.Message}");
                return Json(new { success = false, message = "Error interno del servidor" });
            }
        }


    }

    public class ValoracionRequest
    {
        public int PedidoId { get; set; }
        public int ValoracionGeneral { get; set; }
        public int ValoracionCalidad { get; set; }
        public int ValoracionTiempo { get; set; }
        public string? Comentarios { get; set; }
        public DateTime Fecha { get; set; }
    }

    public class PedidoRequest
    {
        public string TipoServicio { get; set; }

        public string Observaciones { get; set; }

    }

    public class ConfirmarRecogidaRequest
    {
        public int PedidoId { get; set; }
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
    }
}