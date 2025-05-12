using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoIdentity.Datos;
using ProyectoIdentity.Models;
using System.Text.Json;

namespace Proyecto1_MZ_MJ.Controllers
{
    public class RecoleccionController : Controller
    {
        private readonly ApplicationDbContext _context;

        public RecoleccionController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Seleccionar()
        {
            // Verificar si tenemos datos para procesar
            if (!TempData.ContainsKey("ProductosSeleccionados") && !TempData.ContainsKey("DatosCarrito"))
            {
                return RedirectToAction("SeleccionMultiple", "Productos", new { mensaje = "No hay productos seleccionados" });
            }

            // Mantener los datos en TempData
            TempData.Keep("ProductosSeleccionados");
            TempData.Keep("DatosCarrito");

            // Obtener todos los puntos de recolección
            var puntosRecoleccion = await _context.CollectionPoints
                .Include(p => p.Sucursal)
                .ToListAsync();

            // Verificar que existan puntos de recolección
            if (!puntosRecoleccion.Any())
            {
                // Intentar crear puntos de recolección si no existen
                await CrearPuntosRecoleccionPorDefecto();

                // Volver a cargar
                puntosRecoleccion = await _context.CollectionPoints
                    .Include(p => p.Sucursal)
                    .ToListAsync();

                if (!puntosRecoleccion.Any())
                {
                    return RedirectToAction("Index", "Home", new { mensaje = "No hay puntos de recolección disponibles" });
                }
            }

            return View(puntosRecoleccion);
        }

        private async Task CrearPuntosRecoleccionPorDefecto()
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

        [HttpPost]
        public async Task<IActionResult> Confirmar(int id, double? userLat, double? userLng, double? distancia)
        {
            // Mantener los datos en TempData
            TempData.Keep("ProductosSeleccionados");
            TempData.Keep("DatosCarrito");

            // Obtener el punto de recolección
            var puntoRecoleccion = await _context.CollectionPoints
                .Include(p => p.Sucursal)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (puntoRecoleccion == null)
            {
                return NotFound();
            }

            // Pasar todos los valores necesarios a la vista
            ViewBag.PuntoRecoleccionId = id;
            ViewBag.UserLat = userLat ?? -0.1857;
            ViewBag.UserLng = userLng ?? -78.4954;

            // Si se recibió la distancia, usarla; si no, calcularla
            if (distancia.HasValue)
            {
                ViewBag.Distancia = distancia.Value;
            }
            else
            {
                // Calcular la distancia si no se pasó
                double distanciaCalculada = CalcularDistancia(
                    ViewBag.UserLat,
                    ViewBag.UserLng,
                    puntoRecoleccion.Sucursal.Latitud,
                    puntoRecoleccion.Sucursal.Longitud
                );
                ViewBag.Distancia = Math.Round(distanciaCalculada, 2);
            }

            // Si estás trabajando con un pedido específico
            if (TempData.ContainsKey("PedidoTemporalId"))
            {
                ViewBag.PedidoId = TempData["PedidoTemporalId"];
                TempData.Keep("PedidoTemporalId");
            }

            return View(puntoRecoleccion);
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
        public async Task<IActionResult> FinalizarPedido(int puntoRecoleccionId)
        {
            var puntoRecoleccion = await _context.CollectionPoints
                .Include(p => p.Sucursal)
                .FirstOrDefaultAsync(p => p.Id == puntoRecoleccionId);

            if (puntoRecoleccion == null)
            {
                return NotFound();
            }

            int pedidoId = 0;

            // Verificamos si viene de selección múltiple
            if (TempData.ContainsKey("ProductosSeleccionados"))
            {
                string productosJson = TempData["ProductosSeleccionados"].ToString();
                var productosSeleccionados = System.Text.Json.JsonSerializer.Deserialize<List<ProductoSeleccionadoInput>>(productosJson);
                pedidoId = await CrearPedidoDesdeSeleccionMultiple(productosSeleccionados, puntoRecoleccion.SucursalId);
            }
            // Verificamos si viene del carrito
            else if (TempData.ContainsKey("DatosCarrito"))
            {
                string carritoJson = TempData["DatosCarrito"].ToString();
                pedidoId = await CrearPedidoDesdeCarrito(carritoJson, puntoRecoleccion.SucursalId);
            }
            else
            {
                return RedirectToAction("Index", "Home");
            }

            // Guardar el ID del pedido actual para "Ver mi pedido"
            HttpContext.Session.SetInt32("PedidoActualId", pedidoId);

            // Guardar también en una cookie para mayor persistencia
            Response.Cookies.Append("PedidoActualId", pedidoId.ToString(), new CookieOptions
            {
                Expires = DateTimeOffset.Now.AddDays(1)
            });

            return RedirectToAction("Resumen", "Pedidos", new { id = pedidoId });
        }

        private async Task<int> CrearPedidoDesdeSeleccionMultiple(List<ProductoSeleccionadoInput> seleccionados, int sucursalId)
        {
            var pedido = new Pedido
            {
                Fecha = DateTime.Now,
                SucursalId = sucursalId,
                PedidoProductos = new List<PedidoProducto>(),
                Estado = "Preparándose"
            };

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

            return pedido.Id;
        }

        private async Task<int> CrearPedidoDesdeCarrito(string pedidoJson, int sucursalId)
        {
            var itemsCarrito = System.Text.Json.JsonSerializer.Deserialize<List<CarritoItem>>(pedidoJson);

            var pedido = new Pedido
            {
                Fecha = DateTime.Now,
                SucursalId = sucursalId,
                PedidoProductos = new List<PedidoProducto>(),
                Estado = "Preparándose"
            };

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

            // Indicar que se debe limpiar el carrito
            TempData["LimpiarCarrito"] = true;

            return pedido.Id;
        }
    }
}