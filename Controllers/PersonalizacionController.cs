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

        public async Task<IActionResult> Confirmacion(int id)
        {
            try
            {
                Console.WriteLine($"[DEBUG] Confirmacion - Buscando pedido ID: {id}");

                // ✅ OBTENER EL USUARIO ACTUAL
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                // ✅ CARGAR PEDIDO CON LAS RELACIONES POR SEPARADO (como en ResumenAdmin)
                var pedido = await _context.Pedidos
                    .AsNoTracking()
                    .Include(p => p.Sucursal)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (pedido == null)
                {
                    TempData["Error"] = "Pedido no encontrado";
                    return RedirectToAction("Index");
                }

                // ✅ CARGAR LAS RELACIONES POR SEPARADO PARA EVITAR CONFLICTOS
                // Cargar PedidoProductos (para pedidos normales)
                pedido.PedidoProductos = await _context.PedidoProductos
                    .AsNoTracking()
                    .Include(pp => pp.Producto)
                    .Where(pp => pp.PedidoId == pedido.Id)
                    .ToListAsync();

                // Cargar Detalles (para pedidos de personalización)
                pedido.Detalles = await _context.PedidoDetalles
                    .AsNoTracking()
                    .Include(d => d.Producto)
                    .Where(d => d.PedidoId == pedido.Id)
                    .ToListAsync();

                Console.WriteLine($"[DEBUG] Pedido {pedido.Id} - PedidoProductos: {pedido.PedidoProductos.Count}, Detalles: {pedido.Detalles.Count}");

                // ✅ VALIDACIÓN DE SEGURIDAD: Solo el propietario puede ver el pedido
                if (User.Identity.IsAuthenticated)
                {
                    if (!string.IsNullOrEmpty(pedido.UsuarioId) && pedido.UsuarioId != userId)
                    {
                        TempData["Error"] = "No tienes permisos para ver este pedido";
                        return RedirectToAction("Index");
                    }

                    if (string.IsNullOrEmpty(pedido.UsuarioId) && !User.IsInRole("Administrador"))
                    {
                        TempData["Error"] = "No tienes permisos para ver este pedido";
                        return RedirectToAction("Index");
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(pedido.UsuarioId))
                    {
                        TempData["Error"] = "Debes iniciar sesión para ver este pedido";
                        return RedirectToAction("Acceso", "Cuentas");
                    }
                }

                Console.WriteLine("[DEBUG] Confirmacion - Enviando a vista...");
                return View(pedido);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Confirmacion: {ex.Message}");
                TempData["Error"] = "Error al cargar el pedido: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        // Panel de administrador
        [Authorize(Roles = "Administrador")]

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

        // AGREGAR ESTOS MÉTODOS AL PersonalizacionController.cs

        // ============== CRUD PRODUCTOS - SOLO ADMINISTRADORES ==============

        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> AdminProductos()
        {
            var productos = await _context.Productos.ToListAsync();
            return View(productos);
        }

        // REEMPLAZAR los métodos CrearProducto y EditarProducto en PersonalizacionController.cs

        [Authorize(Roles = "Administrador")]
        public IActionResult CrearProducto()
        {
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Administrador")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearProducto(ProductoViewModel model, IFormFile? imagen)
        {
            Console.WriteLine($"Imagen recibida: {imagen?.FileName ?? "NULL"}");
            Console.WriteLine($"Tamaño: {imagen?.Length ?? 0} bytes");
            Console.WriteLine($"Content-Type: {imagen?.ContentType ?? "NULL"}");
            if (ModelState.IsValid)
            {
                try
                {
                    var producto = new Producto
                    {
                        Nombre = model.Nombre,
                        Descripcion = model.Descripcion,
                        Categoria = model.Categoria,
                        Precio = model.Precio,
                        InfoNutricional = model.InfoNutricional,
                        Alergenos = model.Alergenos
                    };

                    // ✅ PROCESAR INGREDIENTES DESDE JAVASCRIPT (Nombre, Costo, Removible)
                    var ingredientesJson = Request.Form["IngredientesJson"].ToString();
                    if (!string.IsNullOrEmpty(ingredientesJson))
                    {
                        try
                        {
                            // Validar que el JSON sea válido antes de guardarlo
                            var testParse = JsonSerializer.Deserialize<List<dynamic>>(ingredientesJson);
                            producto.Ingredientes = ingredientesJson;
                        }
                        catch (JsonException)
                        {
                            // Si el JSON es inválido, no guardamos ingredientes
                            producto.Ingredientes = null;
                        }
                    }

                    // Procesar imagen si se subió una
                    if (imagen != null && imagen.Length > 0)
                    {
                        Console.WriteLine($"Procesando imagen: {imagen.FileName}, {imagen.Length} bytes");

                        try
                        {
                            using (var memoryStream = new MemoryStream())
                            {
                                await imagen.CopyToAsync(memoryStream);
                                producto.Imagen = memoryStream.ToArray();// ← Aquí se convierte a byte[]
                                Console.WriteLine($"Imagen guardada: {producto.Imagen.Length} bytes");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error al procesar imagen: {ex.Message}");
                        }
                    }
                    else
                    {
                        Console.WriteLine("No se recibió imagen o está vacía");
                    }

                    _context.Productos.Add(producto);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Producto creado exitosamente";
                    return RedirectToAction("AdminProductos");
                }
                catch (Exception ex)
                {
                    TempData["Error"] = "Error al crear el producto: " + ex.Message;
                }
            }

            return View(model);
        }

        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> EditarProducto(int id)
        {
            var producto = await _context.Productos.FindAsync(id);
            if (producto == null) return NotFound();

            var model = new ProductoViewModel
            {
                Id = producto.Id,
                Nombre = producto.Nombre,
                Descripcion = producto.Descripcion,
                Categoria = producto.Categoria,
                Precio = producto.Precio,
                InfoNutricional = producto.InfoNutricional,
                Alergenos = producto.Alergenos,
                ImagenExistente = producto.Imagen,
                IngredientesJson = producto.Ingredientes // ✅ PASAR JSON COMPLETO A LA VISTA
            };

            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "Administrador")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarProducto(ProductoViewModel model, IFormFile? imagen)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var producto = await _context.Productos.FindAsync(model.Id);
                    if (producto == null) return NotFound();

                    producto.Nombre = model.Nombre;
                    producto.Descripcion = model.Descripcion;
                    producto.Categoria = model.Categoria;
                    producto.Precio = model.Precio;
                    producto.InfoNutricional = model.InfoNutricional;
                    producto.Alergenos = model.Alergenos;

                    // ✅ PROCESAR INGREDIENTES DESDE JAVASCRIPT (Nombre, Costo, Removible)
                    var ingredientesJson = Request.Form["IngredientesJson"].ToString();
                    if (!string.IsNullOrEmpty(ingredientesJson))
                    {
                        try
                        {
                            // Validar que el JSON sea válido antes de guardarlo
                            var testParse = JsonSerializer.Deserialize<List<dynamic>>(ingredientesJson);
                            producto.Ingredientes = ingredientesJson;
                        }
                        catch (JsonException)
                        {
                            // Si el JSON es inválido, mantener el valor anterior
                            // producto.Ingredientes no se modifica
                        }
                    }
                    else
                    {
                        producto.Ingredientes = null;
                    }

                    // Procesar nueva imagen si se subió una
                    if (imagen != null && imagen.Length > 0)
                    {
                        using (var memoryStream = new MemoryStream())
                        {
                            await imagen.CopyToAsync(memoryStream);
                            producto.Imagen = memoryStream.ToArray();
                        }
                    }

                    _context.Update(producto);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Producto actualizado exitosamente";
                    return RedirectToAction("AdminProductos");
                }
                catch (Exception ex)
                {
                    TempData["Error"] = "Error al actualizar el producto: " + ex.Message;
                }
            }

            // Si hay error, recargar datos existentes
            if (model.Id > 0)
            {
                var productoExistente = await _context.Productos.FindAsync(model.Id);
                if (productoExistente != null)
                {
                    model.ImagenExistente = productoExistente.Imagen;
                    model.IngredientesJson = productoExistente.Ingredientes;
                }
            }

            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> EliminarProducto([FromBody] EliminarProductoRequest request)
        {
            try
            {
                var producto = await _context.Productos.FindAsync(request.Id);
                if (producto == null)
                {
                    return Json(new { success = false, message = "Producto no encontrado" });
                }

                // Verificar si el producto tiene pedidos asociados
                var tienePedidos = await _context.PedidoProductos
                    .AnyAsync(pp => pp.ProductoId == producto.Id);

                if (tienePedidos)
                {
                    return Json(new
                    {
                        success = false,
                        message = "No se puede eliminar el producto porque tiene pedidos asociados"
                    });
                }

                // Verificar si el producto tiene detalles de pedido
                var tieneDetalles = await _context.PedidoDetalles
                    .AnyAsync(pd => pd.ProductoId == producto.Id);

                if (tieneDetalles)
                {
                    return Json(new
                    {
                        success = false,
                        message = "No se puede eliminar el producto porque tiene detalles de pedido asociados"
                    });
                }

                _context.Productos.Remove(producto);
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = $"Producto '{producto.Nombre}' eliminado exitosamente"
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Error al eliminar producto: {ex.Message}");
                return Json(new
                {
                    success = false,
                    message = "Error interno del servidor al eliminar el producto"
                });
            }
        }

        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> DetalleProducto(int id)
        {
            var producto = await _context.Productos.FindAsync(id);
            if (producto == null) return NotFound();

            return View(producto);
        }

        // Método para obtener categorías existentes para el formulario
        [Authorize(Roles = "Administrador")]
        public async Task<JsonResult> ObtenerCategorias()
        {
            var categorias = await _context.Productos
                .Where(p => !string.IsNullOrEmpty(p.Categoria))
                .Select(p => p.Categoria)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            return Json(categorias);
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