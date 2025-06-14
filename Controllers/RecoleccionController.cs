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
        public async Task<IActionResult> Seleccionar()
        {
            try
            {
                // Verificar que tenemos datos de productos seleccionados
                if (!TempData.ContainsKey("ProductosSeleccionados") && !TempData.ContainsKey("DatosCarrito"))
                {
                    TempData["Error"] = "No hay productos seleccionados para procesar";
                    return RedirectToAction("SeleccionMultiple", "Productos");
                }

                // Recuperar puntos de recolección
                var puntosRecoleccion = await _context.CollectionPoints
                    .Include(p => p.Sucursal)
                    .ToListAsync();

                // Verificar que existan puntos de recolección
                if (!puntosRecoleccion.Any())
                {
                    // Crear puntos por defecto
                    await CrearPuntosRecoleccionPorDefecto();

                    // Volver a cargar después de crear los puntos
                    puntosRecoleccion = await _context.CollectionPoints
                        .Include(p => p.Sucursal)
                        .ToListAsync();

                    if (!puntosRecoleccion.Any())
                    {
                        TempData["Error"] = "No hay puntos de recolección disponibles";
                        return RedirectToAction("Index", "Home");
                    }
                }

                // Mantener los datos en TempData para la siguiente acción
                if (TempData.ContainsKey("ProductosSeleccionados"))
                {
                    TempData.Keep("ProductosSeleccionados");
                }
                if (TempData.ContainsKey("DatosCarrito"))
                {
                    TempData.Keep("DatosCarrito");
                }

                return View(puntosRecoleccion);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al cargar puntos de recolección: " + ex.Message;
                return RedirectToAction("SeleccionMultiple", "Productos");
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

        [HttpPost]
        public async Task<IActionResult> Confirmar(int id, double userLat, double userLng, string distancia)
        {
            try
            {
                // Obtener el punto de recolección
                var puntoRecoleccion = await _context.CollectionPoints
                    .Include(p => p.Sucursal)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (puntoRecoleccion == null)
                {
                    TempData["Error"] = "Punto de recolección no encontrado";
                    return RedirectToAction("Seleccionar");
                }

                // Obtener la distancia del parámetro y formatearla exactamente como en la vista Seleccionar
                double distanciaValor;
                if (!string.IsNullOrEmpty(distancia) && double.TryParse(distancia, NumberStyles.Any, CultureInfo.InvariantCulture, out distanciaValor))
                {
                    ViewBag.Distancia = distanciaValor;
                    // Pasamos la misma cadena de texto formateada que viene de la vista Seleccionar
                    ViewBag.DistanciaFormateada = distancia;
                }
                else
                {
                    // Si no se pudo parsear la distancia, la calculamos
                    ViewBag.Distancia = CalcularDistancia(userLat, userLng,
                        puntoRecoleccion.Sucursal.Latitud, puntoRecoleccion.Sucursal.Longitud);
                }

                ViewBag.UserLat = userLat;
                ViewBag.UserLng = userLng;
                ViewBag.PuntoRecoleccionId = id;

                // Mantener los datos en TempData
                if (TempData.ContainsKey("ProductosSeleccionados"))
                {
                    TempData.Keep("ProductosSeleccionados");
                }
                if (TempData.ContainsKey("DatosCarrito"))
                {
                    TempData.Keep("DatosCarrito");
                }

                return View(puntoRecoleccion);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al confirmar punto de recolección: " + ex.Message;
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

        [HttpPost]
        public async Task<IActionResult> FinalizarPedido(int puntoRecoleccionId, string tipoServicio, string observaciones)
        {
            try
            {
                Console.WriteLine($"[DEBUG] FinalizarPedido llamado con:");
                Console.WriteLine($"[DEBUG] puntoRecoleccionId: {puntoRecoleccionId}");
                Console.WriteLine($"[DEBUG] tipoServicio: '{tipoServicio}'");

                var puntoRecoleccion = await _context.CollectionPoints
                    .Include(p => p.Sucursal)
                    .FirstOrDefaultAsync(p => p.Id == puntoRecoleccionId);

                if (puntoRecoleccion == null)
                {
                    TempData["Error"] = "Punto de recolección no encontrado";
                    return RedirectToAction("Seleccionar");
                }

                int pedidoId = 0;

                // Verificamos si viene de selección múltiple
                if (TempData.ContainsKey("ProductosSeleccionados"))
                {
                    string productosJson = TempData["ProductosSeleccionados"].ToString();

                    // Intentar deserializar con ambos tipos para compatibilidad
                    try
                    {
                        // Primero intentar con ElementoCarrito (formato nuevo)
                        var elementosCarrito = Newtonsoft.Json.JsonConvert.DeserializeObject<List<ElementoCarrito>>(productosJson);
                        pedidoId = await CrearPedidoDesdeElementosCarrito(elementosCarrito, puntoRecoleccion.SucursalId);
                    }
                    catch
                    {
                        // Si falla, intentar con el formato anterior
                        var productosSeleccionados = System.Text.Json.JsonSerializer.Deserialize<List<ProductoSeleccionadoInput>>(productosJson);
                        pedidoId = await CrearPedidoDesdeSeleccionMultiple(productosSeleccionados, puntoRecoleccion.SucursalId);
                    }
                }
                // Verificamos si viene del carrito
                else if (TempData.ContainsKey("DatosCarrito"))
                {
                    string carritoJson = TempData["DatosCarrito"].ToString();
                    pedidoId = await CrearPedidoDesdeCarrito(carritoJson, puntoRecoleccion.SucursalId);
                }
                else
                {
                    TempData["Error"] = "No se encontraron datos del pedido";
                    return RedirectToAction("SeleccionMultiple", "Productos");
                }

                if (pedidoId > 0)
                {
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