using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoIdentity.Datos;
using ProyectoIdentity.Models;
using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Newtonsoft.Json;
using System.Security.Claims;

namespace Proyecto1_MZ_MJ.Controllers
{
    [Authorize(Roles = "Administrador,Registrado")]
    public class RecoleccionController : Controller
    {
        private readonly ApplicationDbContext _context;

        public RecoleccionController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Recoleccion/Seleccionar
        public async Task<IActionResult> Seleccionar(bool esPersonalizacion = false)
        {
            try
            {
                // VERIFICAR SI VIENE DE RECOMPENSAS, PRODUCTOS O PERSONALIZACIÓN
                bool vieneDeRecompensas = TempData.ContainsKey("RecompensaCanjeada");
                bool vieneDeProductos = TempData.ContainsKey("ProductosSeleccionados") || TempData.ContainsKey("DatosCarrito");
                bool vieneDePersonalizacion = esPersonalizacion || TempData.ContainsKey("ProductoPersonalizacionId");

                if (!vieneDeRecompensas && !vieneDeProductos && !vieneDePersonalizacion)
                {
                    TempData["Error"] = "No hay productos o recompensas seleccionados para procesar";
                    return RedirectToAction("SeleccionMultiple", "Productos");
                }

                var puntosRecoleccion = await _context.CollectionPoints
                    .Include(p => p.Sucursal)
                    .ToListAsync();

                if (!puntosRecoleccion.Any())
                {
                    await CrearPuntosRecoleccionPorDefecto();
                    puntosRecoleccion = await _context.CollectionPoints
                        .Include(p => p.Sucursal)
                        .ToListAsync();

                    if (!puntosRecoleccion.Any())
                    {
                        TempData["Error"] = "No hay puntos de recolección disponibles";
                        return RedirectToAction("Index", "Home");
                    }
                }

                // MANTENER TODOS LOS DATOS EN TEMPDATA
                if (TempData.ContainsKey("ProductosSeleccionados"))
                {
                    TempData.Keep("ProductosSeleccionados");
                }
                if (TempData.ContainsKey("DatosCarrito"))
                {
                    TempData.Keep("DatosCarrito");
                }
                if (TempData.ContainsKey("RecompensaCanjeada"))
                {
                    TempData.Keep("RecompensaCanjeada");
                }
                // ✅ NUEVO: Mantener datos de personalización
                if (TempData.ContainsKey("ProductoPersonalizacionId"))
                {
                    TempData.Keep("ProductoPersonalizacionId");
                }

                // ✅ NUEVO: Agregar información para la vista
                ViewBag.EsPersonalizacion = vieneDePersonalizacion;
                if (vieneDePersonalizacion)
                {
                    ViewBag.MensajeEspecial = "Selecciona dónde recoger tu pedido personalizado";
                    ViewBag.ProductoId = TempData.Peek("ProductoPersonalizacionId");
                }

                return View(puntosRecoleccion);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al cargar puntos de recolección: " + ex.Message;
                return RedirectToAction("Index", "Home");
            }
        }

        private async Task CrearPuntosRecoleccionPorDefecto()
        {
            try
            {
                // Verificar si existe al menos una sucursal
                var sucursal = await _context.Sucursales.FirstOrDefaultAsync();

                if (sucursal == null)
                {
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

                // Crear puntos de recolección por defecto
                var puntosDefault = new List<CollectionPoint>
                {
                    new CollectionPoint
                    {
                        Name = "Punto Principal - Verace Pizza",
                        Address = "Av. de los Shyris N35-52",
                        Descripcion = "Punto de recolección principal",
                        Latitude = -0.180653,
                        Longitude = -78.487834,
                        SucursalId = sucursal.Id
                    },
                };

                _context.CollectionPoints.AddRange(puntosDefault);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Log del error si es necesario
                throw new Exception("Error al crear puntos de recolección por defecto: " + ex.Message);
            }
        }

        // REEMPLAZAR EL MÉTODO Confirmar ACTUAL CON ESTE:

        [HttpPost]
        public async Task<IActionResult> Confirmar(int id, double userLat, double userLng, string distancia, bool esPersonalizacion = false)
        {
            try
            {

                Console.WriteLine($"[DEBUG RECOLECCION] Distancia recibida: '{distancia}'");
                Console.WriteLine($"[DEBUG RECOLECCION] UserLat: {userLat}, UserLng: {userLng}");
                var puntoRecoleccion = await _context.CollectionPoints
                    .Include(p => p.Sucursal)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (puntoRecoleccion == null)
                {
                    TempData["Error"] = "Punto de recolección no encontrado";
                    return RedirectToAction("Seleccionar");
                }

                // Calcular distancia
                double distanciaValor;
                if (!string.IsNullOrEmpty(distancia) && double.TryParse(distancia, NumberStyles.Any, CultureInfo.InvariantCulture, out distanciaValor))
                {
                    ViewBag.Distancia = distanciaValor;
                    ViewBag.DistanciaFormateada = distancia; // ✅ USA EL STRING ORIGINAL
                    Console.WriteLine($"[DEBUG RECOLECCION] Parsing exitoso: {distanciaValor}"); // ✅ AGREGAR ESTA LÍNEA

                }
                else
                {
                    ViewBag.Distancia = CalcularDistancia(userLat, userLng,
                        puntoRecoleccion.Sucursal.Latitud, puntoRecoleccion.Sucursal.Longitud);
                    Console.WriteLine($"[DEBUG RECOLECCION] Parsing falló, calculado: {ViewBag.Distancia}"); // ✅ AGREGAR ESTA LÍNEA

                }
                Console.WriteLine($"[DEBUG RECOLECCION] Distancia final: {ViewBag.Distancia}"); // ✅ AGREGAR ESTA LÍNEA

                // Configurar ViewBag
                ViewBag.UserLat = userLat;
                ViewBag.UserLng = userLng;
                ViewBag.PuntoRecoleccionId = id;

                // ✅ VERIFICAR SI ES PERSONALIZACIÓN Y CONFIGURAR ViewBag
                if (esPersonalizacion || TempData.ContainsKey("ProductoPersonalizacionId"))
                {
                    ViewBag.EsPersonalizacion = true;
                    var productoId = TempData["ProductoPersonalizacionId"];
                    if (productoId != null)
                    {
                        ViewBag.ProductoPersonalizacionId = productoId;
                        // Mantener el ID para la siguiente acción
                        TempData.Keep("ProductoPersonalizacionId");
                    }
                }
                else
                {
                    ViewBag.EsPersonalizacion = false;
                }

                // Mantener otros datos en TempData
                if (TempData.ContainsKey("ProductosSeleccionados"))
                {
                    TempData.Keep("ProductosSeleccionados");
                }
                if (TempData.ContainsKey("DatosCarrito"))
                {
                    TempData.Keep("DatosCarrito");
                }

                // ✅ AHORA SIEMPRE MOSTRAR LA PANTALLA DE CONFIRMACIÓN
                return View(puntoRecoleccion);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al confirmar punto de recolección: " + ex.Message;
                return RedirectToAction("Seleccionar");
            }
        }

        [HttpPost]
        public IActionResult ContinuarPersonalizacion(int puntoRecoleccionId, int productoId)
        {
            try
            {
                var puntoRecoleccion = _context.CollectionPoints
                    .Include(p => p.Sucursal)
                    .FirstOrDefault(p => p.Id == puntoRecoleccionId);

                if (puntoRecoleccion == null)
                {
                    TempData["Error"] = "Punto de recolección no encontrado";
                    return RedirectToAction("Seleccionar");
                }

                // ✅ GUARDAR DATOS EN SESIÓN (sin tipoServicio por ahora)
                HttpContext.Session.SetInt32("SucursalSeleccionada", puntoRecoleccion.SucursalId);
                HttpContext.Session.SetInt32("PuntoRecoleccionSeleccionado", puntoRecoleccionId);
                HttpContext.Session.SetString("PuntoRecoleccionNombre", puntoRecoleccion.Name);

                return RedirectToAction("Detalle", "Personalizacion", new { id = productoId });
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al continuar: " + ex.Message;
                return RedirectToAction("Seleccionar");
            }
        }

        // Método auxiliar para calcular la distancia
        private double CalcularDistancia(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371; // Radio de la Tierra en kilómetros

            double dLat = ToRadians(lat2 - lat1);
            double dLon = ToRadians(lon2 - lon1);

            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return R * c;
        }

        private double ToRadians(double degrees)
        {
            return degrees * Math.PI / 180;
        }

        // ACTUALIZA el método FinalizarPedido en RecoleccionController.cs
        // Agrega estas líneas de debug al inicio del método, justo después de la declaración de variables:

        [HttpPost]
        public async Task<IActionResult> FinalizarPedido(int puntoRecoleccionId, string tipoServicio, string observaciones)
        {
            try
            {
                Console.WriteLine($"[DEBUG] FinalizarPedido llamado con:");
                Console.WriteLine($"[DEBUG] puntoRecoleccionId: {puntoRecoleccionId}");
                Console.WriteLine($"[DEBUG] tipoServicio: '{tipoServicio}'");

                // ✅ AGREGAR DEBUG PARA TEMPDATA
                Console.WriteLine($"[DEBUG] TempData keys disponibles:");
                foreach (var key in TempData.Keys)
                {
                    Console.WriteLine($"[DEBUG] - {key}");
                }

                // ✅ DEBUG ESPECÍFICO PARA RECOMPENSAS
                if (TempData.ContainsKey("RecompensaCanjeada"))
                {
                    string recompensaJson = TempData["RecompensaCanjeada"].ToString();
                    Console.WriteLine($"[DEBUG] RecompensaCanjeada JSON: {recompensaJson}");
                }

                var puntoRecoleccion = await _context.CollectionPoints
                    .Include(p => p.Sucursal)
                    .FirstOrDefaultAsync(p => p.Id == puntoRecoleccionId);

                if (puntoRecoleccion == null)
                {
                    TempData["Error"] = "Punto de recolección no encontrado";
                    return RedirectToAction("Seleccionar");
                }

                int pedidoId = 0;

                // VERIFICAR SI VIENE DE RECOMPENSAS
                if (TempData.ContainsKey("RecompensaCanjeada"))
                {
                    Console.WriteLine($"[DEBUG] Procesando recompensa canjeada...");
                    string recompensaJson = TempData["RecompensaCanjeada"].ToString();
                    pedidoId = await CrearPedidoDesdeRecompensa(recompensaJson, puntoRecoleccion.SucursalId, tipoServicio);
                    Console.WriteLine($"[DEBUG] Pedido creado desde recompensa con ID: {pedidoId}");
                }
                // VERIFICAR SI VIENE DE PRODUCTOS (tu lógica existente)
                else if (TempData.ContainsKey("ProductosSeleccionados"))
                {
                    Console.WriteLine($"[DEBUG] Procesando productos seleccionados...");
                    string productosJson = TempData["ProductosSeleccionados"].ToString();

                    // Intentar deserializar con ambos tipos para compatibilidad
                    try
                    {
                        // Primero intentar con ElementoCarrito (formato nuevo)
                        var elementosCarrito = Newtonsoft.Json.JsonConvert.DeserializeObject<List<ElementoCarrito>>(productosJson);
                        pedidoId = await CrearPedidoDesdeElementosCarrito(elementosCarrito, puntoRecoleccion.SucursalId, tipoServicio);
                    }
                    catch
                    {
                        // Si falla, intentar con el formato anterior
                        var productosSeleccionados = System.Text.Json.JsonSerializer.Deserialize<List<ProductoSeleccionadoInput>>(productosJson);
                        pedidoId = await CrearPedidoDesdeSeleccionMultiple(productosSeleccionados, puntoRecoleccion.SucursalId, tipoServicio);
                    }
                }
                // Verificamos si viene del carrito
                else if (TempData.ContainsKey("DatosCarrito"))
                {
                    Console.WriteLine($"[DEBUG] Procesando datos del carrito...");
                    string carritoJson = TempData["DatosCarrito"].ToString();
                    pedidoId = await CrearPedidoDesdeCarrito(carritoJson, puntoRecoleccion.SucursalId, tipoServicio);
                }
                else
                {
                    Console.WriteLine($"[DEBUG] ERROR: No se encontraron datos del pedido o recompensa");
                    TempData["Error"] = "No se encontraron datos del pedido o recompensa";
                    return RedirectToAction("Seleccionar");
                }

                if (pedidoId > 0)
                {
                    Console.WriteLine($"[DEBUG] ✅ Pedido creado exitosamente con ID: {pedidoId}");

                    // Guardar el ID del pedido actual para "Ver mi pedido"
                    HttpContext.Session.SetInt32("PedidoActualId", pedidoId);

                    // Guardar también en una cookie para mayor persistencia
                    Response.Cookies.Append("PedidoActualId", pedidoId.ToString(), new CookieOptions
                    {
                        Expires = DateTimeOffset.Now.AddDays(1)
                    });

                    TempData["Success"] = "Pedido creado exitosamente";
                    return RedirectToAction("Resumen", "Pedidos", new { id = pedidoId });
                }
                else
                {
                    Console.WriteLine($"[DEBUG] ❌ Error: pedidoId = 0");
                    TempData["Error"] = "Error al crear el pedido";
                    return RedirectToAction("Seleccionar");
                }
            }
            catch (Exception ex)
            {
                // AGREGAR ESTAS LÍNEAS PARA VER EL ERROR REAL:
                Console.WriteLine($"[ERROR COMPLETO] {ex.Message}");
                Console.WriteLine($"[STACK TRACE] {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"[INNER EXCEPTION] {ex.InnerException.Message}");
                }

                TempData["Error"] = "Error al finalizar pedido: " + ex.Message;
                return RedirectToAction("Seleccionar");
            }
        }
        // Método para crear pedido desde ElementoCarrito (formato nuevo)
        // 1. Método CrearPedidoDesdeElementosCarrito CORREGIDO:
        private async Task<int> CrearPedidoDesdeElementosCarrito(List<ElementoCarrito> elementos, int sucursalId, string tipoServicio = null, string observaciones = null)
        {
            try
            {
                Console.WriteLine($"[DEBUG] CrearPedidoDesdeElementosCarrito - Usuario autenticado: {User.Identity.IsAuthenticated}");

                var pedido = new Pedido
                {
                    Fecha = DateTime.Now,
                    SucursalId = sucursalId,
                    PedidoProductos = new List<PedidoProducto>(),
                    Estado = "Preparándose",
                    UsuarioId = User.Identity.IsAuthenticated ? User.FindFirstValue(ClaimTypes.NameIdentifier) : null,
                    TipoServicio = string.IsNullOrEmpty(tipoServicio) ? null : tipoServicio
                };

                Console.WriteLine($"[DEBUG] Pedido creado con UsuarioId: {pedido.UsuarioId}");

                _context.Pedidos.Add(pedido);
                await _context.SaveChangesAsync();

                decimal total = 0;

                foreach (var item in elementos)
                {
                    var producto = await _context.Productos.FindAsync(item.ProductoId);
                    if (producto != null)
                    {
                        decimal subtotal = producto.Precio * item.Cantidad;
                        total += subtotal;

                        var pedidoProducto = new PedidoProducto
                        {
                            PedidoId = pedido.Id,
                            ProductoId = producto.Id,
                            Cantidad = item.Cantidad,
                            Precio = producto.Precio
                        };

                        _context.PedidoProductos.Add(pedidoProducto);
                    }
                }

                pedido.Total = total;
                _context.Update(pedido);
                await _context.SaveChangesAsync();

                Console.WriteLine($"[DEBUG] Total calculado: {total}");

                // ✅ AGREGAR PUNTOS AL USUARIO SI ESTÁ AUTENTICADO
                if (User.Identity.IsAuthenticated && total > 0)
                {
                    Console.WriteLine($"[DEBUG] Llamando AgregarPuntosAUsuario");
                    await AgregarPuntosAUsuario(pedido.UsuarioId, total);
                }
                else
                {
                    Console.WriteLine($"[DEBUG] NO se agregaron puntos - Autenticado: {User.Identity.IsAuthenticated}, Total: {total}");
                }

                return pedido.Id;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Error en CrearPedidoDesdeElementosCarrito: {ex.Message}");
                return 0;
            }
        }

        // 2. Método CrearPedidoDesdeSeleccionMultiple CORREGIDO:
        private async Task<int> CrearPedidoDesdeSeleccionMultiple(List<ProductoSeleccionadoInput> seleccionados, int sucursalId, string tipoServicio = null, string observaciones = null)
        {
            try
            {
                Console.WriteLine($"[DEBUG] CrearPedidoDesdeSeleccionMultiple - Usuario autenticado: {User.Identity.IsAuthenticated}");

                var pedido = new Pedido
                {
                    Fecha = DateTime.Now,
                    SucursalId = sucursalId,
                    PedidoProductos = new List<PedidoProducto>(),
                    Estado = "Preparándose",
                    UsuarioId = User.Identity.IsAuthenticated ? User.FindFirstValue(ClaimTypes.NameIdentifier) : null,
                    TipoServicio = string.IsNullOrEmpty(tipoServicio) ? null : tipoServicio
                };

                Console.WriteLine($"[DEBUG] Pedido creado con UsuarioId: {pedido.UsuarioId}");

                _context.Pedidos.Add(pedido);
                await _context.SaveChangesAsync();

                decimal total = 0;

                foreach (var item in seleccionados)
                {
                    var producto = await _context.Productos.FindAsync(item.ProductoId);
                    if (producto != null)
                    {
                        decimal subtotal = producto.Precio * item.Cantidad;
                        total += subtotal;

                        var pedidoProducto = new PedidoProducto
                        {
                            PedidoId = pedido.Id,
                            ProductoId = producto.Id,
                            Cantidad = item.Cantidad,
                            Precio = producto.Precio
                        };

                        _context.PedidoProductos.Add(pedidoProducto);
                    }
                }

                pedido.Total = total;
                _context.Update(pedido);
                await _context.SaveChangesAsync();

                Console.WriteLine($"[DEBUG] Total calculado: {total}");

                // ✅ AGREGAR PUNTOS AL USUARIO SI ESTÁ AUTENTICADO
                if (User.Identity.IsAuthenticated && total > 0)
                {
                    Console.WriteLine($"[DEBUG] Llamando AgregarPuntosAUsuario");
                    await AgregarPuntosAUsuario(pedido.UsuarioId, total);
                }
                else
                {
                    Console.WriteLine($"[DEBUG] NO se agregaron puntos - Autenticado: {User.Identity.IsAuthenticated}, Total: {total}");
                }

                return pedido.Id;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Error en CrearPedidoDesdeSeleccionMultiple: {ex.Message}");
                return 0;
            }
        }

        // 3. Método CrearPedidoDesdeCarrito CORREGIDO:
        private async Task<int> CrearPedidoDesdeCarrito(string pedidoJson, int sucursalId, string tipoServicio = null, string observaciones = null)
        {
            try
            {
                Console.WriteLine($"[DEBUG] CrearPedidoDesdeCarrito - Usuario autenticado: {User.Identity.IsAuthenticated}");

                var itemsCarrito = System.Text.Json.JsonSerializer.Deserialize<List<CarritoItem>>(pedidoJson);

                var pedido = new Pedido
                {
                    Fecha = DateTime.Now,
                    SucursalId = sucursalId,
                    PedidoProductos = new List<PedidoProducto>(),
                    Estado = "Preparándose",
                    UsuarioId = User.Identity.IsAuthenticated ? User.FindFirstValue(ClaimTypes.NameIdentifier) : null,
                    TipoServicio = string.IsNullOrEmpty(tipoServicio) ? null : tipoServicio
                };

                Console.WriteLine($"[DEBUG] Pedido creado con UsuarioId: {pedido.UsuarioId}");

                _context.Pedidos.Add(pedido);
                await _context.SaveChangesAsync();

                decimal total = 0;

                foreach (var item in itemsCarrito)
                {
                    if (int.TryParse(item.Id, out int productoId))
                    {
                        var producto = await _context.Productos.FindAsync(productoId);
                        if (producto != null)
                        {
                            decimal subtotal = item.Precio * item.Cantidad;
                            total += subtotal;

                            var pedidoProducto = new PedidoProducto
                            {
                                PedidoId = pedido.Id,
                                ProductoId = productoId,
                                Cantidad = item.Cantidad,
                                Precio = item.Precio
                            };

                            _context.PedidoProductos.Add(pedidoProducto);
                        }
                    }
                }

                pedido.Total = total;
                _context.Update(pedido);
                await _context.SaveChangesAsync();

                Console.WriteLine($"[DEBUG] Total calculado: {total}");

                // ✅ AGREGAR PUNTOS AL USUARIO SI ESTÁ AUTENTICADO
                if (User.Identity.IsAuthenticated && total > 0)
                {
                    Console.WriteLine($"[DEBUG] Llamando AgregarPuntosAUsuario");
                    await AgregarPuntosAUsuario(pedido.UsuarioId, total);
                }
                else
                {
                    Console.WriteLine($"[DEBUG] NO se agregaron puntos - Autenticado: {User.Identity.IsAuthenticated}, Total: {total}");
                }

                // Indicar que se debe limpiar el carrito
                TempData["LimpiarCarrito"] = true;

                return pedido.Id;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Error en CrearPedidoDesdeCarrito: {ex.Message}");
                return 0;
            }
        }

        private async Task AgregarPuntosAUsuario(string usuarioId, decimal totalPedido)
        {
            Console.WriteLine($"[DEBUG] AgregarPuntosAUsuario iniciado - UsuarioId: {usuarioId}, Total: {totalPedido}");

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
                    Descripcion = $"Puntos ganados por pedido - Total: ${totalPedido:F2}",
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

        // REEMPLAZA el método CrearPedidoDesdeRecompensa en RecoleccionController.cs

        private async Task<int> CrearPedidoDesdeRecompensa(string recompensaJson, int sucursalId, string tipoServicio = null)
        {
            try
            {
                Console.WriteLine($"[DEBUG] CrearPedidoDesdeRecompensa - JSON recibido: {recompensaJson}");

                var datosRecompensa = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(recompensaJson);

                // Extraer los datos del JSON
                var recompensaId = datosRecompensa.GetProperty("RecompensaId").GetInt32();
                var usuarioId = datosRecompensa.GetProperty("UsuarioId").GetString();
                var nombreRecompensa = datosRecompensa.GetProperty("NombreRecompensa").GetString();
                var puntosUtilizados = datosRecompensa.GetProperty("PuntosUtilizados").GetInt32();
                var historialCanjeId = datosRecompensa.GetProperty("HistorialCanjeId").GetInt32();

                Console.WriteLine($"[DEBUG] Datos parseados - RecompensaId: {recompensaId}, Usuario: {usuarioId}, Nombre: {nombreRecompensa}");

                var pedido = new Pedido
                {
                    Fecha = DateTime.Now,
                    SucursalId = sucursalId,
                    Estado = "Preparándose",
                    UsuarioId = usuarioId,
                    TipoServicio = string.IsNullOrEmpty(tipoServicio) ? "Para llevar" : tipoServicio,
                    Total = 0, // Las recompensas no tienen costo monetario
                    EsCupon = true, // Marcar que es una recompensa
                    PedidoProductos = new List<PedidoProducto>()
                };

                _context.Pedidos.Add(pedido);
                await _context.SaveChangesAsync();

                // ✅ CREAR UNA ENTRADA EN PEDIDOPRODUCTOS PARA LA RECOMPENSA
                // Esto es importante para que aparezca en el resumen del pedido

                // Obtener el producto asociado a la recompensa
                var productoRecompensa = await _context.ProductosRecompensa
                    .Include(pr => pr.Producto)
                    .FirstOrDefaultAsync(pr => pr.Id == recompensaId);

                if (productoRecompensa?.Producto != null)
                {
                    var pedidoProducto = new PedidoProducto
                    {
                        PedidoId = pedido.Id,
                        ProductoId = productoRecompensa.ProductoId ?? 0,
                        Cantidad = 1,
                        Precio = 0, // Precio 0 porque es una recompensa
                                    // ✅ AGREGAR CAMPO PERSONALIZADO SI EXISTE
                                    // EsRecompensa = true, // Si tienes este campo en tu modelo
                    };

                    _context.PedidoProductos.Add(pedidoProducto);
                    await _context.SaveChangesAsync();

                    Console.WriteLine($"[DEBUG] PedidoProducto creado para recompensa - ProductoId: {productoRecompensa.ProductoId}");
                }

                // ✅ OPCIONAL: ACTUALIZAR EL HISTORIAL DE CANJE CON EL ID DEL PEDIDO
                var historialCanje = await _context.HistorialCanjes.FindAsync(historialCanjeId);
                if (historialCanje != null)
                {
                    // Si tienes un campo PedidoId en HistorialCanje, descomenta esto:
                    // historialCanje.PedidoId = pedido.Id;
                    // await _context.SaveChangesAsync();
                }

                Console.WriteLine($"[DEBUG] Pedido de recompensa creado exitosamente con ID: {pedido.Id}");
                return pedido.Id;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Error en CrearPedidoDesdeRecompensa: {ex.Message}");
                Console.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");
                return 0;
            }
        }

        // Clases DTO necesarias (agrégalas a tu carpeta Models/DTOs)
        public class ElementoCarrito
        {
            public int ProductoId { get; set; }
            public int Cantidad { get; set; }
        }

        public class ProductoSeleccionadoInput
        {
            public int ProductoId { get; set; }
            public bool Seleccionado { get; set; }
            public int Cantidad { get; set; } = 1;
        }

        public class CarritoItem
        {
            public string Id { get; set; }
            public string Nombre { get; set; }
            public decimal Precio { get; set; }
            public int Cantidad { get; set; }
        }
    }
}