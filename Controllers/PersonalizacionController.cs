using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoIdentity.Datos;
using ProyectoIdentity.Models;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

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

        // ============== MÉTODOS PRINCIPALES ==============

        // ✅ INDEX CON VALIDACIÓN DE LÍMITES
        public async Task<IActionResult> Index(string categoria = "todas")
        {
            // ✅ VALIDACIÓN DE LÍMITE DE PEDIDOS
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userId))
            {
                var (countActivos, pedidosActivos) = await ContarPedidosActivos(userId);

                if (countActivos >= 3)
                {
                    var viewModelLimite = CrearViewModelLimite(countActivos, pedidosActivos);
                    return View("LimiteAlcanzado", viewModelLimite);
                }

                ViewBag.PedidosActivos = countActivos;
                ViewBag.LimiteMaximo = 3;
            }

            // Obtener productos
            var productos = await _context.Productos.ToListAsync();

            // Filtrar por categoría si se especifica
            if (!string.IsNullOrEmpty(categoria) && categoria != "todas")
            {
                productos = productos.Where(p => p.Categoria.ToLower() == categoria.ToLower()).ToList();
            }

            // Obtener categorías para el menú
            var categorias = await _context.Productos
                .Where(p => !string.IsNullOrEmpty(p.Categoria))
                .Select(p => p.Categoria)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            ViewBag.Categorias = categorias;
            ViewBag.CategoriaActual = categoria;

            return View(productos);
        }

        // ✅ INICIAR PERSONALIZACIÓN CON VALIDACIÓN
        public async Task<IActionResult> IniciarPersonalizacion(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userId))
            {
                var (countActivos, pedidosActivos) = await ContarPedidosActivos(userId);

                if (countActivos >= 3)
                {
                    TempData["Error"] = $"Has alcanzado el límite de {countActivos}/3 pedidos activos. Espera a que se entreguen para hacer más pedidos.";
                    return RedirectToAction("Index");
                }
            }

            TempData["ProductoPersonalizacionId"] = id;
            return RedirectToAction("Seleccionar", "Recoleccion", new { esPersonalizacion = true });
        }

        // ✅ DETALLE DEL PRODUCTO
        public async Task<IActionResult> Detalle(int id)
        {
            var sucursalSeleccionada = HttpContext.Session.GetInt32("SucursalSeleccionada");

            if (sucursalSeleccionada == null)
            {
                return RedirectToAction("IniciarPersonalizacion", new { id = id });
            }

            var producto = await _context.Productos.FindAsync(id);
            if (producto == null) return NotFound();

            ViewBag.Ingredientes = GetIngredientesProducto(producto);
            return View(producto);
        }

        // ============== VALIDACIONES DE LÍMITES ==============
        // ✅ SOLUCIÓN CORRECTA - Manejando int e int? por separado
        private async Task<(bool permitido, int productosActuales, string mensaje)> ValidarLimiteProductos(string usuarioId)
        {
            if (string.IsNullOrEmpty(usuarioId))
                return (true, 0, "");

            // Obtener todos los pedidos activos del usuario
            var pedidosActivos = await _context.Pedidos
                .Where(p => p.UsuarioId == usuarioId &&
                           (p.Estado == "Preparándose" || p.Estado == "Listo para entregar"))
                .ToListAsync();

            int totalProductos = 0;

            // Contar productos en cada pedido
            foreach (var pedido in pedidosActivos)
            {
                // Contar en Detalles (personalización) - Cantidad es INT
                var detalles = await _context.PedidoDetalles
                    .Where(d => d.PedidoId == pedido.Id)
                    .ToListAsync();
                totalProductos += detalles.Sum(d => d.Cantidad); // ✅ SIN ?? porque es int

                // Contar en PedidoProductos (pedidos normales) - Cantidad es INT?
                var productos = await _context.PedidoProductos
                    .Where(pp => pp.PedidoId == pedido.Id)
                    .ToListAsync();
                totalProductos += productos.Sum(pp => pp.Cantidad ?? 0); // ✅ CON ?? porque es int?
            }

            if (totalProductos >= 3)
            {
                return (false, totalProductos, $"Ya tienes {totalProductos}/3 productos en pedidos activos. Espera a que se entreguen para pedir más.");
            }

            return (true, totalProductos, "");
        }
        private async Task<(bool permitido, int productosDisponibles, string mensaje)> ValidarAgregarProductos(string usuarioId, int cantidadAAgregar)
        {
            var (permitido, productosActuales, mensaje) = await ValidarLimiteProductos(usuarioId);

            if (!permitido)
                return (false, 0, mensaje);

            int productosDisponibles = 3 - productosActuales;

            if (cantidadAAgregar > productosDisponibles)
            {
                return (false, productosDisponibles,
                    $"Solo puedes agregar {productosDisponibles} producto(s) más. Actualmente tienes {productosActuales}/3 productos en pedidos activos.");
            }

            return (true, productosDisponibles, "");
        }

        // ============== GESTIÓN DE CARRITO ==============

        // ✅ AGREGAR AL CARRITO CON VALIDACIONES
        [HttpPost]
        public async Task<IActionResult> AgregarAlCarrito([FromBody] PersonalizacionRequest request)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                // Validar límite de productos
                if (!string.IsNullOrEmpty(userId))
                {
                    var (permitido, productosDisponibles, mensaje) = await ValidarAgregarProductos(userId, request.Cantidad);
                    if (!permitido)
                    {
                        return Json(new { success = false, message = mensaje });
                    }
                }

                var producto = await _context.Productos.FindAsync(request.ProductoId);
                if (producto == null)
                    return Json(new { success = false, message = "Producto no encontrado" });

                var carrito = GetCarritoPersonalizado();

                // Verificar límite en el carrito actual
                int productosEnCarrito = carrito.Sum(c => c.Cantidad);
                if (productosEnCarrito + request.Cantidad > 3)
                {
                    int disponibles = 3 - productosEnCarrito;
                    return Json(new
                    {
                        success = false,
                        message = $"Solo puedes agregar {disponibles} producto(s) más al carrito. Límite: 3 productos por pedido."
                    });
                }

                // Calcular ahorro para administradores
                decimal ahorroInterno = 0;
                if (User.IsInRole("Administrador") && request.IngredientesRemovidos?.Any() == true)
                {
                    ahorroInterno = request.IngredientesRemovidos.Count * 0.50m * request.Cantidad;
                }

                var itemCarrito = new ItemCarritoPersonalizado
                {
                    Id = request.ProductoId,
                    Nombre = producto.Nombre,
                    Precio = producto.Precio,
                    Cantidad = request.Cantidad,
                    IngredientesRemovidos = request.IngredientesRemovidos ?? new List<string>(),
                    NotasEspeciales = request.NotasEspeciales ?? "",
                    AhorroInterno = ahorroInterno,
                    Subtotal = producto.Precio * request.Cantidad
                };

                carrito.Add(itemCarrito);
                SetCarritoPersonalizado(carrito);

                return Json(new
                {
                    success = true,
                    message = "Producto agregado",
                    totalItems = carrito.Sum(c => c.Cantidad),
                    totalCarrito = carrito.Sum(c => c.Subtotal),
                    productosRestantes = 3 - carrito.Sum(c => c.Cantidad)
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ✅ OBTENER LÍMITES PRODUCTOS (para JavaScript)
        [HttpGet]
        public async Task<IActionResult> ObtenerLimitesProductos()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Json(new { productosActuales = 0, limite = 3, disponibles = 3 });

            var (permitido, productosActuales, mensaje) = await ValidarLimiteProductos(userId);
            int disponibles = 3 - productosActuales;

            return Json(new
            {
                productosActuales = productosActuales,
                limite = 3,
                disponibles = Math.Max(0, disponibles),
                mensaje = mensaje
            });
        }

        // ✅ VER CARRITO
        public IActionResult VerCarrito()
        {
            try
            {
                var carrito = GetCarritoPersonalizado();

                // Validar que todos los items tengan datos válidos
                foreach (var item in carrito)
                {
                    if (item.Subtotal <= 0 && item.Precio > 0 && item.Cantidad > 0)
                    {
                        item.Subtotal = item.Precio * item.Cantidad;
                    }
                }

                return View(carrito);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Error en VerCarrito: {ex.Message}");
                return View(new List<ItemCarritoPersonalizado>());
            }
        }

        // ✅ ACTUALIZAR CARRITO
        [HttpPost]
        public IActionResult ActualizarCarrito([FromBody] List<ItemCarritoPersonalizado> carrito)
        {
            try
            {
                // Validar items antes de guardar
                foreach (var item in carrito)
                {
                    if (item.Subtotal <= 0 && item.Precio > 0 && item.Cantidad > 0)
                    {
                        item.Subtotal = item.Precio * item.Cantidad;
                    }
                }

                SetCarritoPersonalizado(carrito);
                return Json(new { success = true, totalItems = carrito.Sum(c => c.Cantidad) });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ============== PROCESAMIENTO DE PEDIDOS ==============

        // ✅ PROCESAR PEDIDO CON VALIDACIONES
        [HttpPost]
        public async Task<IActionResult> ProcesarPedido([FromBody] PedidoPersonalizacionRequest request)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var carritoItems = GetCarritoPersonalizado();

                if (carritoItems == null || !carritoItems.Any())
                {
                    return Json(new { success = false, message = "El carrito está vacío" });
                }

                // Validar límite de productos en el carrito
                int totalProductos = carritoItems.Sum(item => item.Cantidad);
                if (totalProductos > 3)
                {
                    return Json(new
                    {
                        success = false,
                        message = $"No puedes procesar un pedido con {totalProductos} productos. Máximo permitido: 3 productos."
                    });
                }

                // Validar límite global de productos activos
                if (!string.IsNullOrEmpty(userId))
                {
                    var (permitido, productosActuales, mensaje) = await ValidarLimiteProductos(userId);
                    if (!permitido)
                    {
                        return Json(new { success = false, message = mensaje });
                    }

                    if (productosActuales + totalProductos > 3)
                    {
                        return Json(new
                        {
                            success = false,
                            message = $"No puedes procesar este pedido. Tienes {productosActuales} productos activos + {totalProductos} en carrito = {productosActuales + totalProductos} total. Máximo: 3 productos."
                        });
                    }
                }

                // Validar items válidos
                var itemsValidos = carritoItems.Where(item =>
                    item.Id > 0 &&
                    item.Cantidad > 0 &&
                    item.Precio > 0 &&
                    !string.IsNullOrEmpty(item.Nombre)
                ).ToList();

                if (!itemsValidos.Any())
                {
                    return Json(new { success = false, message = "No hay productos válidos en el carrito" });
                }

                // Recalcular subtotales
                foreach (var item in itemsValidos)
                {
                    item.Subtotal = item.Precio * item.Cantidad;
                }

                var total = itemsValidos.Sum(item => item.Subtotal);

                if (total <= 0)
                {
                    return Json(new { success = false, message = "El total del pedido no es válido" });
                }

                // Obtener sucursal
                var sucursal = await _context.Sucursales.FirstOrDefaultAsync();
                if (sucursal == null)
                {
                    return Json(new { success = false, message = "No hay sucursales disponibles" });
                }

                // Crear el pedido
                var pedido = new Pedido
                {
                    UsuarioId = userId,
                    SucursalId = sucursal.Id,
                    TipoServicio = request.TipoServicio,
                    Fecha = DateTime.Now,
                    Estado = "Preparándose",
                    Total = total,
                    Detalles = itemsValidos.Select(item => new PedidoDetalle
                    {
                        ProductoId = item.Id,
                        PrecioUnitario = item.Precio,
                        Cantidad = item.Cantidad,
                        IngredientesRemovidos = System.Text.Json.JsonSerializer.Serialize(item.IngredientesRemovidos),
                        NotasEspeciales = item.NotasEspeciales
                    }).ToList()
                };

                _context.Pedidos.Add(pedido);
                await _context.SaveChangesAsync();

                // Agregar puntos al usuario
                if (User.Identity.IsAuthenticated && total > 0)
                {
                    await AgregarPuntosAUsuarioPersonalizacion(userId, total);
                }

                // Limpiar carrito
                LimpiarCarritoPersonalizado();

                return Json(new
                {
                    success = true,
                    pedidoId = pedido.Id,
                    total = total,
                    puntosGanados = (int)(total * 30),
                    productosEnPedido = totalProductos
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Error en ProcesarPedido: {ex.Message}");
                return Json(new { success = false, message = "Error interno del servidor: " + ex.Message });
            }
        }

        // ============== CONFIRMACIÓN Y SEGUIMIENTO ==============

        public async Task<IActionResult> Confirmacion(int id)
        {
            try
            {
                Console.WriteLine($"[DEBUG] Confirmacion - Buscando pedido ID: {id}");

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                var pedido = await _context.Pedidos
                    .AsNoTracking()
                    .Include(p => p.Sucursal)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (pedido == null)
                {
                    TempData["Error"] = "Pedido no encontrado";
                    return RedirectToAction("Index");
                }

                // Cargar PedidoProductos
                pedido.PedidoProductos = await _context.PedidoProductos
                    .AsNoTracking()
                    .Include(pp => pp.Producto)
                    .Where(pp => pp.PedidoId == pedido.Id)
                    .ToListAsync();

                // Cargar Detalles
                pedido.Detalles = await _context.PedidoDetalles
                    .AsNoTracking()
                    .Include(d => d.Producto)
                    .Where(d => d.PedidoId == pedido.Id)
                    .ToListAsync();

                Console.WriteLine($"[DEBUG] Pedido {pedido.Id} - PedidoProductos: {pedido.PedidoProductos.Count}, Detalles: {pedido.Detalles.Count}");

                // Validación de seguridad
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

        [HttpPost]
        public async Task<IActionResult> GuardarValoracion([FromBody] ValoracionRequest request)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                var pedido = await _context.Pedidos
                    .FirstOrDefaultAsync(p => p.Id == request.PedidoId && p.UsuarioId == userId);

                if (pedido == null)
                {
                    return Json(new { success = false, message = "Pedido no encontrado" });
                }

                var valoracionExistente = await _context.Valoraciones
                    .FirstOrDefaultAsync(v => v.PedidoId == request.PedidoId);

                if (valoracionExistente != null)
                {
                    return Json(new { success = false, message = "Este pedido ya ha sido valorado" });
                }

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

        // ============== ADMINISTRACIÓN DE PRODUCTOS ==============

        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> AdminProductos()
        {
            var productos = await _context.Productos.ToListAsync();
            return View(productos);
        }

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

                    var ingredientesJson = Request.Form["IngredientesJson"].ToString();
                    if (!string.IsNullOrEmpty(ingredientesJson))
                    {
                        try
                        {
                            var testParse = JsonSerializer.Deserialize<List<dynamic>>(ingredientesJson);
                            producto.Ingredientes = ingredientesJson;
                        }
                        catch (JsonException)
                        {
                            producto.Ingredientes = null;
                        }
                    }

                    if (imagen != null && imagen.Length > 0)
                    {
                        Console.WriteLine($"Procesando imagen: {imagen.FileName}, {imagen.Length} bytes");

                        try
                        {
                            using (var memoryStream = new MemoryStream())
                            {
                                await imagen.CopyToAsync(memoryStream);
                                producto.Imagen = memoryStream.ToArray();
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
                IngredientesJson = producto.Ingredientes
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

                    var ingredientesJson = Request.Form["IngredientesJson"].ToString();
                    if (!string.IsNullOrEmpty(ingredientesJson))
                    {
                        try
                        {
                            var testParse = JsonSerializer.Deserialize<List<dynamic>>(ingredientesJson);
                            producto.Ingredientes = ingredientesJson;
                        }
                        catch (JsonException)
                        {
                            // Si el JSON es inválido, mantener el valor anterior
                        }
                    }
                    else
                    {
                        producto.Ingredientes = null;
                    }

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

        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> AdminAnalisis()
        {
            var analisis = await GenerarAnalisisSimple();
            return View(analisis);
        }

        // ============== MÉTODOS DE CARRITO (PRIVADOS) ==============

        private List<ItemCarritoPersonalizado> GetCarritoPersonalizado()
        {
            try
            {
                var carritoJson = HttpContext.Session.GetString("CarritoPersonalizado");
                if (string.IsNullOrEmpty(carritoJson))
                {
                    return new List<ItemCarritoPersonalizado>();
                }

                var carrito = System.Text.Json.JsonSerializer.Deserialize<List<ItemCarritoPersonalizado>>(carritoJson);
                return carrito ?? new List<ItemCarritoPersonalizado>();
            }
            catch
            {
                return new List<ItemCarritoPersonalizado>();
            }
        }

        private void SetCarritoPersonalizado(List<ItemCarritoPersonalizado> carrito)
        {
            try
            {
                var carritoJson = System.Text.Json.JsonSerializer.Serialize(carrito);
                HttpContext.Session.SetString("CarritoPersonalizado", carritoJson);
            }
            catch
            {
                // Error al guardar
            }
        }

        private void LimpiarCarritoPersonalizado()
        {
            HttpContext.Session.Remove("CarritoPersonalizado");
            Console.WriteLine("[DEBUG] Carrito personalizado limpiado");
        }

        private void GuardarCarritoEnSession(List<ItemCarritoPersonalizado> carrito)
        {
            SetCarritoPersonalizado(carrito);
        }

        private void LimpiarCarritoDeSession()
        {
            LimpiarCarritoPersonalizado();
        }

        // ============== MÉTODOS HELPER ==============

        private async Task<(int count, List<Pedido> pedidosActivos)> ContarPedidosActivos(string usuarioId)
        {
            if (string.IsNullOrEmpty(usuarioId))
                return (0, new List<Pedido>());

            var pedidosActivos = await _context.Pedidos
                .Where(p => p.UsuarioId == usuarioId &&
                           (p.Estado == "Preparándose" || p.Estado == "Listo para entregar"))
                .OrderByDescending(p => p.Fecha)
                .ToListAsync();

            return (pedidosActivos.Count, pedidosActivos);
        }

        private LimiteAlcanzadoViewModel CrearViewModelLimite(int countActivos, List<Pedido> pedidosActivos)
        {
            return new LimiteAlcanzadoViewModel
            {
                PedidosActivos = countActivos,
                LimiteMaximo = 3,
                PedidosPendientes = pedidosActivos.Select(p => new PedidoPendienteInfo
                {
                    Id = p.Id,
                    Fecha = p.Fecha,
                    Total = p.Total,
                    Estado = p.Estado,
                    TipoServicio = p.TipoServicio
                }).ToList()
            };
        }

        private async Task AgregarPuntosAUsuarioPersonalizacion(string usuarioId, decimal totalPedido)
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

            int puntosGanados = (int)(totalPedido * 30);
            Console.WriteLine($"[DEBUG] Puntos a agregar: {puntosGanados}");

            int puntosAnteriores = usuario.PuntosFidelidad ?? 0;
            usuario.PuntosFidelidad = puntosAnteriores + puntosGanados;

            Console.WriteLine($"[DEBUG] Puntos anteriores: {puntosAnteriores}, Nuevos puntos: {usuario.PuntosFidelidad}");

            try
            {
                var transaccion = new TransaccionPuntos
                {
                    UsuarioId = usuarioId,
                    Puntos = puntosGanados,
                    Tipo = "Ganancia",
                    Descripcion = $"Puntos ganados por pedido personalizado - Total: ${totalPedido:F2}",
                    Fecha = DateTime.Now
                };

                _context.TransaccionesPuntos.Add(transaccion);
                await _context.SaveChangesAsync();

                Console.WriteLine("[DEBUG] ✅ Cambios guardados exitosamente en la base de datos");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Error al guardar en la base de datos: {ex.Message}");
                Console.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");
            }
        }

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

    }

    // ============== MODELOS DE REQUEST Y CLASES ==============

    public class PersonalizacionRequest
    {
        public int ProductoId { get; set; }
        public int Cantidad { get; set; } = 1;
        public List<string> IngredientesRemovidos { get; set; } = new();
        public string? NotasEspeciales { get; set; }
    }

    public class PedidoPersonalizacionRequest
    {
        public string TipoServicio { get; set; } = "";
        public string Observaciones { get; set; } = "";
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

    public class EliminarProductoRequest
    {
        public int Id { get; set; }
    }

    public class ConfirmarRecogidaRequest
    {
        public int PedidoId { get; set; }
    }

    public class PedidoRequest
    {
        public string TipoServicio { get; set; } = "";
        public string Observaciones { get; set; } = "";
    }

    public class PedidoPersonalizadoRequest
    {
        public string TipoServicio { get; set; } = "";
    }

    public class AnalisisSimple
    {
        public string NombreIngrediente { get; set; } = "";
        public string NombreProducto { get; set; } = "";
        public decimal CostoUnitario { get; set; }
        public int VecesRemovido { get; set; }
        public decimal AhorroTotal { get; set; }
    }

    public class LimiteAlcanzadoViewModel
    {
        public int PedidosActivos { get; set; }
        public int LimiteMaximo { get; set; }
        public List<PedidoPendienteInfo> PedidosPendientes { get; set; } = new();
        public string MensajePersonalizado =>
            $"Tienes {PedidosActivos} de {LimiteMaximo} pedidos activos. Espera a que se entreguen para hacer más pedidos.";
    }

    public class PedidoPendienteInfo
    {
        public int Id { get; set; }
        public DateTime Fecha { get; set; }
        public decimal Total { get; set; }
        public string Estado { get; set; } = "";
        public string TipoServicio { get; set; } = "";

        public string FechaFormateada => Fecha.ToString("dd/MM/yyyy HH:mm");
        public string EstadoBadgeClass => Estado switch
        {
            "Preparándose" => "bg-warning",
            "Listo para entregar" => "bg-success",
            _ => "bg-secondary"
        };
    }
}