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
        public async Task<IActionResult> Personalizacion()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userId))
            {
                var (permitido, productosActivos, productosCarritos, disponibles, mensaje) = await ValidarLimitesGlobales(userId);

                // ✅ NO BLOQUEAR AQUÍ - Solo mostrar info
                ViewBag.ProductosActivos = productosActivos;
                ViewBag.Disponibles = disponibles;
                ViewBag.LimiteMaximo = 3;
            }

            var categorias = await _context.Productos
                .Where(p => !string.IsNullOrEmpty(p.Categoria))
                .Select(p => p.Categoria)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            ViewBag.Categorias = categorias;
            return View();
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

        public async Task<IActionResult> IniciarPersonalizacion(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // ✅ VALIDAR LÍMITES ANTES DE MOSTRAR LA PERSONALIZACIÓN
            if (!string.IsNullOrEmpty(userId))
            {
                var (permitido, productosActivos, productosCarritos, disponibles, mensaje) = await ValidarLimitesGlobales(userId);
                if (!permitido)
                {
                    var viewModel = new LimiteAlcanzadoViewModel
                    {
                        PedidosActivos = productosActivos,
                        LimiteMaximo = 3,
                        PedidosPendientes = new List<PedidoPendienteInfo>()
                    };
                    return View("../Shared/LimiteAlcanzado", viewModel);
                }
            }

            var producto = await _context.Productos.FindAsync(id);
            if (producto == null)
                return NotFound();

            // ✅ CAMBIAR A DETALLE DIRECTAMENTE
            HttpContext.Session.SetInt32("SucursalSeleccionada", 1); // Sucursal por defecto
            return RedirectToAction("Detalle", new { id = id });
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

                // ✅ VALIDAR LÍMITES UNIFICADOS
                var (permitido, disponibles, mensaje) = await ValidarAgregarProductosUnificado(userId, request.Cantidad);
                if (!permitido)
                {
                    return Json(new
                    {
                        success = false,
                        message = mensaje,
                        redirectUrl = Url.Action("Personalizacion") // ✅ REDIRIGIR A PERSONALIZACION
                    });
                }

                var producto = await _context.Productos.FindAsync(request.ProductoId);
                if (producto == null)
                    return Json(new { success = false, message = "Producto no encontrado" });

                var carrito = GetCarritoPersonalizacion();

                // ✅ VALIDAR LÍMITE EN CARRITOS COMBINADOS
                int productosEnCarritos = ContarProductosEnCarritos(userId);
                if (productosEnCarritos + request.Cantidad > disponibles + productosEnCarritos)
                {
                    return Json(new
                    {
                        success = false,
                        message = $"Solo puedes agregar {disponibles} producto(s) más considerando ambos carritos."
                    });
                }

                // Calcular precio con descuentos
                decimal precioFinal = producto.Precio;
                decimal ahorroTotal = 0;

                if (request.IngredientesRemovidos?.Any() == true)
                {
                    foreach (var ingredienteRemovido in request.IngredientesRemovidos)
                    {
                        var descuento = await _context.DescuentosIngredientes
                            .FirstOrDefaultAsync(d => d.NombreIngrediente == ingredienteRemovido);
                        if (descuento != null)
                        {
                            ahorroTotal += descuento.MontoDescuento;
                        }
                    }
                    precioFinal = Math.Max(0, producto.Precio - ahorroTotal);
                }

                var itemCarrito = new ItemCarritoPersonalizado
                {
                    Id = request.ProductoId,
                    Nombre = producto.Nombre,
                    Precio = precioFinal,
                    Cantidad = request.Cantidad,
                    IngredientesRemovidos = request.IngredientesRemovidos ?? new List<string>(),
                    NotasEspeciales = request.NotasEspeciales,
                    AhorroInterno = ahorroTotal,
                    Subtotal = precioFinal * request.Cantidad
                };

                carrito.Add(itemCarrito);
                SetCarritoPersonalizacion(carrito);

                // ✅ OBTENER LÍMITES ACTUALIZADOS
                var (_, productosActivos, productosCarritos, disponiblesActualizados, _) = await ValidarLimitesGlobales(userId);

                return Json(new
                {
                    success = true,
                    message = "Producto agregado al carrito",
                    totalItems = carrito.Sum(c => c.Cantidad),
                    totalCarrito = carrito.Sum(c => c.Subtotal),
                    ahorro = ahorroTotal,
                    // ✅ INFORMACIÓN UNIFICADA
                    disponibles = disponiblesActualizados,
                    productosActivos = productosActivos,
                    productosEnCarritos = productosCarritos
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        private void SetCarritoPersonalizacion(List<ItemCarritoPersonalizado> carrito)
        {
            try
            {
                var carritoJson = JsonSerializer.Serialize(carrito);
                HttpContext.Session.SetString("CarritoPersonalizacion", carritoJson);
                Console.WriteLine($"[DEBUG] Carrito guardado: {carrito.Count} items");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Error al guardar carrito: {ex.Message}");
            }
        }


        // ✅ MÉTODO ObtenerLimitesProductos CORREGIDO (Sin error de Json.Serialize)
        // ✅ MÉTODO ObtenerLimitesProductos CON NOMBRES CORREGIDOS
        [HttpGet]
        public async Task<IActionResult> ObtenerLimitesProductos()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            try
            {
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new
                    {
                        productosActivos = 0,
                        limite = 3,
                        disponibles = 3,
                        productosEnCarritos = 0,
                        totalOcupados = 0,
                        mensaje = "",
                        permitido = true
                    });
                }

                // ✅ USAR EL MÉTODO CORREGIDO
                var (permitido, productosActivos, productosCarritos, disponibles, mensaje) = await ValidarLimitesGlobales(userId);

                // ✅ ASEGURAR QUE NO HAY VALORES NULL O NEGATIVOS
                productosActivos = Math.Max(0, productosActivos);
                productosCarritos = Math.Max(0, productosCarritos);
                disponibles = Math.Max(0, disponibles);

                var resultado = new
                {
                    productosActivos = productosActivos,    // ✅ NOMBRE CORRECTO
                    limite = 3,
                    disponibles = disponibles,
                    productosEnCarritos = productosCarritos, // ✅ NOMBRE CORRECTO
                    totalOcupados = productosActivos + productosCarritos,
                    mensaje = mensaje ?? "",
                    permitido = permitido
                };

                // ✅ LOG SIMPLE PARA EVITAR ERRORES
                Console.WriteLine($"[DEBUG] ObtenerLimitesProductos - Activos: {productosActivos}, Carritos: {productosCarritos}, Disponibles: {disponibles}, Permitido: {permitido}");

                return Json(resultado);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Error en ObtenerLimitesProductos: {ex.Message}");

                // ✅ DEVOLVER VALORES SEGUROS EN CASO DE ERROR
                return Json(new
                {
                    productosActivos = 0,
                    limite = 3,
                    disponibles = 3,
                    productosEnCarritos = 0,
                    totalOcupados = 0,
                    mensaje = "Error al obtener límites",
                    permitido = true
                });
            }
        }

        // ✅ MÉTODOS AUXILIARES PARA CARRITOS (AGREGAR SOLO SI NO EXISTEN)
        private List<ItemCarritoPersonalizado> GetCarritoPersonalizacion()
        {
            try
            {
                var carritoJson = HttpContext.Session.GetString("CarritoPersonalizacion");
                Console.WriteLine($"[DEBUG] Carrito JSON: {carritoJson}");

                if (string.IsNullOrEmpty(carritoJson))
                    return new List<ItemCarritoPersonalizado>();

                var carrito = JsonSerializer.Deserialize<List<ItemCarritoPersonalizado>>(carritoJson);
                Console.WriteLine($"[DEBUG] Items en carrito: {carrito?.Count ?? 0}");
                return carrito ?? new List<ItemCarritoPersonalizado>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Error al obtener carrito: {ex.Message}");
                return new List<ItemCarritoPersonalizado>();
            }
        }

        private List<ItemCarritoPersonalizado> GetCarritoRecomendacion()
        {
            try
            {
                var carritoJson = HttpContext.Session.GetString("CarritoRecomendacion");
                if (string.IsNullOrEmpty(carritoJson))
                    return new List<ItemCarritoPersonalizado>();

                return JsonSerializer.Deserialize<List<ItemCarritoPersonalizado>>(carritoJson) ?? new List<ItemCarritoPersonalizado>();
            }
            catch
            {
                return new List<ItemCarritoPersonalizado>();
            }
        }

        // ✅ VER CARRITO
        public IActionResult VerCarrito()
        {
            var carrito = GetCarritoPersonalizacion();
            Console.WriteLine($"[DEBUG] VerCarrito - Items: {carrito.Count}");

            foreach (var item in carrito)
            {
                Console.WriteLine($"[DEBUG] Item: {item.Nombre}, Cantidad: {item.Cantidad}, Precio: {item.Precio}");
            }

            return View(carrito);
        }

        // ✅ ACTUALIZAR CARRITO
        // ✅ ACTUALIZAR CARRITO MEJORADO
        [HttpPost]
        public IActionResult ActualizarCarrito([FromBody] List<ItemCarritoPersonalizado> carrito)
        {
            try
            {
                Console.WriteLine($"[DEBUG] ActualizarCarrito recibido: {carrito?.Count ?? 0} items");

                // Si el carrito es null o vacío, limpiar la sesión
                if (carrito == null || !carrito.Any())
                {
                    Console.WriteLine("[DEBUG] Carrito vacío - limpiando sesión");
                    LimpiarCarritoPersonalizacion();
                    return Json(new
                    {
                        success = true,
                        totalItems = 0,
                        message = "Carrito vaciado exitosamente"
                    });
                }

                // Validar y limpiar items inválidos
                var itemsValidos = carrito.Where(item =>
                    item != null &&
                    item.Id > 0 &&
                    item.Cantidad > 0 &&
                    item.Precio >= 0 &&
                    !string.IsNullOrEmpty(item.Nombre)
                ).ToList();

                Console.WriteLine($"[DEBUG] Items válidos: {itemsValidos.Count} de {carrito.Count}");

                // Recalcular subtotales para items válidos
                foreach (var item in itemsValidos)
                {
                    if (item.Subtotal <= 0 && item.Precio > 0 && item.Cantidad > 0)
                    {
                        item.Subtotal = item.Precio * item.Cantidad;
                        Console.WriteLine($"[DEBUG] Recalculado subtotal para {item.Nombre}: {item.Subtotal}");
                    }
                }

                // Guardar carrito actualizado
                SetCarritoPersonalizacion(itemsValidos);

                int totalItems = itemsValidos.Sum(c => c.Cantidad);
                decimal totalCarrito = itemsValidos.Sum(c => c.Subtotal);

                Console.WriteLine($"[DEBUG] Carrito actualizado: {totalItems} items, total: ${totalCarrito}");

                return Json(new
                {
                    success = true,
                    totalItems = totalItems,
                    totalCarrito = totalCarrito,
                    message = "Carrito actualizado exitosamente"
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Error en ActualizarCarrito: {ex.Message}");
                return Json(new
                {
                    success = false,
                    message = "Error al actualizar el carrito: " + ex.Message
                });
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
                var carritoItems = GetCarritoPersonalizacion(); // ✅ CAMBIAR AQUÍ

                Console.WriteLine($"[DEBUG] Carrito obtenido: {carritoItems?.Count ?? 0} items");

                if (carritoItems == null || !carritoItems.Any())
                {
                    Console.WriteLine("[DEBUG] Carrito está vacío o es null");
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

                // Validar límite global usando el método unificado
                if (!string.IsNullOrEmpty(userId))
                {
                    var (permitido, productosActivos, productosCarritos, disponibles, mensaje) = await ValidarLimitesGlobales(userId);
                    if (!permitido)
                    {
                        return Json(new { success = false, message = mensaje });
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
                    Console.WriteLine("[DEBUG] No hay items válidos en el carrito");
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

                // Limpiar carrito usando el método correcto
                LimpiarCarritoPersonalizacion(); // ✅ CAMBIAR AQUÍ TAMBIÉN

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
        private void LimpiarCarritoPersonalizacion()
        {
            try
            {
                HttpContext.Session.Remove("CarritoPersonalizacion");
                Console.WriteLine("[DEBUG] Carrito de personalización limpiado");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Error al limpiar carrito: {ex.Message}");
            }
        }

        private async Task<(int totalProductos, List<Pedido> pedidosActivos)> ContarTodosLosProductosActivos2(string usuarioId)
        {
            if (string.IsNullOrEmpty(usuarioId))
                return (0, new List<Pedido>());

            var pedidosActivos = await _context.Pedidos
                .Where(p => p.UsuarioId == usuarioId &&
                           p.Estado == "Entregado") // ✅ SOLO PREPARÁNDOSE
                .OrderByDescending(p => p.Fecha)
                .ToListAsync();

            int totalProductos = 0;
            foreach (var pedido in pedidosActivos)
            {
                var detalles = await _context.PedidoDetalles
                    .Where(d => d.PedidoId == pedido.Id)
                    .ToListAsync();
                totalProductos += detalles.Sum(d => d.Cantidad);

                var productos = await _context.PedidoProductos
                    .Where(pp => pp.PedidoId == pedido.Id)
                    .ToListAsync();
                totalProductos += productos.Sum(pp => pp.Cantidad ?? 0);
            }

            return (totalProductos, pedidosActivos);
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

        private async Task<(int totalProductos, List<Pedido> pedidosActivos)> ContarTodosLosProductosActivos(string usuarioId)
        {
            if (string.IsNullOrEmpty(usuarioId))
                return (0, new List<Pedido>());

            var pedidosActivos = await _context.Pedidos
                .Where(p => p.UsuarioId == usuarioId &&
                           (p.Estado == "Preparándose" || p.Estado == "Listo para entregar"))
                .OrderByDescending(p => p.Fecha)
                .ToListAsync();

            int totalProductos = 0;

            // ✅ CONTAR PRODUCTOS DE AMBOS TIPOS DE PEDIDOS
            foreach (var pedido in pedidosActivos)
            {
                // Productos de personalización (PedidoDetalles)
                var detalles = await _context.PedidoDetalles
                    .Where(d => d.PedidoId == pedido.Id)
                    .ToListAsync();
                totalProductos += detalles.Sum(d => d.Cantidad);

                // Productos de pedidos normales (PedidoProductos)
                var productos = await _context.PedidoProductos
                    .Where(pp => pp.PedidoId == pedido.Id)
                    .ToListAsync();
                totalProductos += productos.Sum(pp => pp.Cantidad ?? 0);
            }

            Console.WriteLine($"[DEBUG] Usuario {usuarioId}: {totalProductos} productos activos en {pedidosActivos.Count} pedidos");

            return (totalProductos, pedidosActivos);
        }

        // ✅ MÉTODO UNIFICADO PARA CONTAR PRODUCTOS EN CARRITOS
        private int ContarProductosEnCarritos(string usuarioId)
        {
            int productosEnCarritos = 0;

            try
            {
                // Productos en carrito de personalización
                var carritoPersonalizacion = GetCarritoPersonalizacion();
                productosEnCarritos += carritoPersonalizacion?.Sum(c => c.Cantidad) ?? 0;

                // Productos en carrito de recomendación IA
                var carritoRecomendacion = GetCarritoRecomendacion();
                productosEnCarritos += carritoRecomendacion?.Sum(c => c.Cantidad) ?? 0;

                Console.WriteLine($"[DEBUG] Productos en carritos: Personalización={carritoPersonalizacion?.Sum(c => c.Cantidad) ?? 0}, Recomendación={carritoRecomendacion?.Sum(c => c.Cantidad) ?? 0}, Total={productosEnCarritos}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Error al contar productos en carritos: {ex.Message}");
            }

            return productosEnCarritos;
        }

        // ✅ MÉTODO ValidarLimitesGlobales CORREGIDO PARA INCLUIR CARRITO ACTUAL
        private async Task<(bool permitido, int productosActivos, int productosCarritos, int disponibles, string mensaje)> ValidarLimitesGlobales(string usuarioId)
        {
            const int LIMITE_MAXIMO = 3;

            try
            {
                if (string.IsNullOrEmpty(usuarioId))
                    return (true, 0, 0, LIMITE_MAXIMO, "");

                // ✅ 1. CONTAR PRODUCTOS EN PEDIDOS ACTIVOS
                var pedidosActivos = await _context.Pedidos
                    .Where(p => p.UsuarioId == usuarioId &&
                               (p.Estado == "Preparándose" || p.Estado == "Listo para entregar"))
                    .ToListAsync();

                int productosActivos = 0;

                foreach (var pedido in pedidosActivos)
                {
                    try
                    {
                        // Productos de personalización (PedidoDetalles)
                        var detalles = await _context.PedidoDetalles
                            .Where(d => d.PedidoId == pedido.Id)
                            .ToListAsync();
                        productosActivos += detalles.Sum(d => Math.Max(0, d.Cantidad));

                        // Productos de pedidos normales (PedidoProductos)
                        var productos = await _context.PedidoProductos
                            .Where(pp => pp.PedidoId == pedido.Id)
                            .ToListAsync();
                        productosActivos += productos.Sum(pp => Math.Max(0, pp.Cantidad ?? 0));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[ERROR] Error contando productos del pedido {pedido.Id}: {ex.Message}");
                    }
                }

                // ✅ 2. CONTAR PRODUCTOS EN CARRITOS ACTUALES
                int productosCarritos = 0;
                try
                {
                    // Carrito de personalización
                    var carritoPersonalizacion = GetCarritoPersonalizacion();
                    if (carritoPersonalizacion != null)
                    {
                        productosCarritos += carritoPersonalizacion.Sum(c => Math.Max(0, c.Cantidad));
                    }

                    // Carrito de recomendación IA
                    var carritoRecomendacion = GetCarritoRecomendacion();
                    if (carritoRecomendacion != null)
                    {
                        productosCarritos += carritoRecomendacion.Sum(c => Math.Max(0, c.Cantidad));
                    }

                    Console.WriteLine($"[DEBUG] Productos en carrito personalización: {carritoPersonalizacion?.Sum(c => c.Cantidad) ?? 0}");
                    Console.WriteLine($"[DEBUG] Productos en carrito recomendación: {carritoRecomendacion?.Sum(c => c.Cantidad) ?? 0}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] Error contando productos en carritos: {ex.Message}");
                    productosCarritos = 0;
                }

                // ✅ 3. CALCULAR DISPONIBLES CONSIDERANDO AMBOS (PEDIDOS + CARRITOS)
                int totalOcupados = productosActivos + productosCarritos;
                int disponibles = Math.Max(0, LIMITE_MAXIMO - totalOcupados);
                bool permitido = disponibles > 0;

                // ✅ 4. MENSAJE UNIFICADO
                string mensaje = "";
                if (!permitido)
                {
                    if (productosActivos >= LIMITE_MAXIMO)
                    {
                        mensaje = $"Tienes {productosActivos} productos en pedidos activos. Espera a que se entreguen.";
                    }
                    else if (totalOcupados >= LIMITE_MAXIMO)
                    {
                        mensaje = $"Límite alcanzado: {productosActivos} en pedidos activos + {productosCarritos} en carritos = {totalOcupados}/3 productos.";
                    }
                }

                // ✅ 5. LOG DETALLADO PARA DEBUG
                Console.WriteLine($"[DEBUG] ValidarLimitesGlobales - Usuario: {usuarioId}");
                Console.WriteLine($"[DEBUG] Productos en pedidos activos: {productosActivos}");
                Console.WriteLine($"[DEBUG] Productos en carritos: {productosCarritos}");
                Console.WriteLine($"[DEBUG] Total ocupados: {totalOcupados}");
                Console.WriteLine($"[DEBUG] Disponibles: {disponibles}");
                Console.WriteLine($"[DEBUG] Permitido: {permitido}");

                return (permitido, productosActivos, productosCarritos, disponibles, mensaje);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Error en ValidarLimitesGlobales: {ex.Message}");
                // ✅ DEVOLVER VALORES SEGUROS EN CASO DE ERROR
                return (true, 0, 0, LIMITE_MAXIMO, "Error al validar límites");
            }
        }
        // ✅ MÉTODO UNIFICADO PARA VALIDAR AGREGAR PRODUCTOS
        private async Task<(bool permitido, int disponibles, string mensaje)> ValidarAgregarProductosUnificado(string usuarioId, int cantidadAAgregar)
        {
            var (permitidoGlobal, productosActivos, productosCarritos, disponibles, mensajeGlobal) = await ValidarLimitesGlobales(usuarioId);

            if (!permitidoGlobal)
                return (false, 0, mensajeGlobal);

            if (cantidadAAgregar > disponibles)
            {
                string mensaje = $"Solo puedes agregar {disponibles} producto(s) más. " +
                                $"Límite: 3 productos total. " +
                                $"Actualmente: {productosActivos} activos + {productosCarritos} en carritos = {productosActivos + productosCarritos}/3";
                return (false, disponibles, mensaje);
            }

            return (true, disponibles, "");
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
        public string MensajeUnificado { get; set; } = "";
        public int ProductosEnPersonalizacion { get; set; }
        public int ProductosEnRecomendacion { get; set; }
        public List<PedidoPendienteInfo> PedidosPendientes { get; set; } = new();

        public string MensajePersonalizado =>
            $"Límite alcanzado: {PedidosActivos}/{LimiteMaximo} productos. " +
            $"Carritos: Personalización ({ProductosEnPersonalizacion}) + Recomendación IA ({ProductosEnRecomendacion}). " +
            "Espera a que se entreguen tus pedidos para continuar.";
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