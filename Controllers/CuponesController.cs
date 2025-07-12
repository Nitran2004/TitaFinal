using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ProyectoIdentity.Datos;
using ProyectoIdentity.Models;
using System.Security.Claims;
using Newtonsoft.Json;
using Mailjet.Client.Resources;

namespace Proyecto1_MZ_MJ.Controllers
{
    [Authorize]

    public class CuponesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CuponesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Vista pública para mostrar cupones disponibles
        public async Task<IActionResult> Index()
        {
            var cupones = await _context.Cupones
                .Where(c => c.Activo && (c.FechaExpiracion == null || c.FechaExpiracion > DateTime.Now))
                .ToListAsync();

            // Filtrar cupones según el día actual
            var cuponesDelDia = FiltrarCuponesPorDia(cupones);

            return View(cuponesDelDia);
        }

        // Vista de detalle individual del cupón
        public async Task<IActionResult> Detalle(int id)
        {
            var cupon = await _context.Cupones
                .FirstOrDefaultAsync(c => c.Id == id && c.Activo);

            if (cupon == null)
            {
                return NotFound();
            }

            // Verificar si el cupón aplica para hoy
            if (!CuponAplicaHoy(cupon))
            {
                ViewBag.NoAplicaHoy = true;
                ViewBag.MensajeNoAplica = "Este cupón no es válido para el día de hoy";
            }

            // Verificar si está expirado
            if (cupon.FechaExpiracion.HasValue && cupon.FechaExpiracion < DateTime.Now)
            {
                ViewBag.Expirado = true;
                ViewBag.MensajeExpirado = "Este cupón ha expirado";
            }

            return View(cupon);
        }

        // Vista para escanear QR (solo Admin y Cajero)
        [Authorize(Roles = "Administrador,Cajero")]
        public IActionResult EscanearQR()
        {
            return View();
        }

        // Procesar código QR escaneado
        [HttpPost]
        [Authorize(Roles = "Administrador,Cajero")]
        public async Task<IActionResult> ProcesarQR(string codigoQR)
        {
            if (string.IsNullOrEmpty(codigoQR))
            {
                return Json(new { success = false, mensaje = "Código QR vacío" });
            }

            try
            {
                // Buscar el cupón por código QR
                var cupon = await _context.Cupones
                    .FirstOrDefaultAsync(c => c.CodigoQR == codigoQR && c.Activo);

                if (cupon == null)
                {
                    return Json(new { success = false, mensaje = "Cupón no encontrado o inactivo" });
                }

                // Verificar fecha de expiración
                if (cupon.FechaExpiracion.HasValue && cupon.FechaExpiracion < DateTime.Now)
                {
                    return Json(new { success = false, mensaje = "Cupón expirado" });
                }

                // Verificar si aplica para el día actual
                if (!CuponAplicaHoy(cupon))
                {
                    return Json(new { success = false, mensaje = "Cupón no válido para hoy" });
                }

                // Obtener productos aplicables
                var productos = await ObtenerProductosAplicables(cupon);

                if (!productos.Any())
                {
                    return Json(new { success = false, mensaje = "No hay productos disponibles para este cupón" });
                }

                return Json(new
                {
                    success = true,
                    cupon = new
                    {
                        id = cupon.Id,
                        nombre = cupon.Nombre,
                        descripcion = cupon.Descripcion,
                        tipoDescuento = cupon.TipoDescuento,
                        valorDescuento = cupon.ValorDescuento,
                        productos = productos.Select(p => new {
                            id = p.Id,
                            nombre = p.Nombre,
                            precio = p.Precio,
                            categoria = p.Categoria,
                            imagen = p.Imagen
                        })
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, mensaje = "Error al procesar cupón: " + ex.Message });
            }
        }

        [HttpPost]
        [Authorize(Roles = "Administrador,Cajero")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcesarCodigoManual(string codigoQR)
        {
            if (string.IsNullOrEmpty(codigoQR))
            {
                TempData["Error"] = "Ingresa un código válido";
                return RedirectToAction("EscanearQR");
            }

            try
            {
                // ✅ EXTRAER USUARIO DEL CÓDIGO QR
                string usuarioId = null;
                string codigoCuponLimpio = codigoQR;

                // Si el código tiene formato: PROMO15-ABC123|USER:usuario123
                if (codigoQR.Contains("|USER:"))
                {
                    var partes = codigoQR.Split("|USER:");
                    codigoCuponLimpio = partes[0]; // Código del cupón
                    usuarioId = partes[1]; // ID del usuario

                    // Verificar que el usuario existe
                    var usuarioExiste = await _context.AppUsuario.AnyAsync(u => u.Id == usuarioId);
                    if (!usuarioExiste)
                    {
                        TempData["Error"] = "Usuario no encontrado en el sistema";
                        return RedirectToAction("EscanearQR");
                    }

                    // Guardar el UsuarioId en TempData para usar después
                    TempData["UsuarioQueEscanea"] = usuarioId;
                    Console.WriteLine($"[DEBUG] Usuario identificado: {usuarioId}");
                }

                // Buscar cupón con el código limpio
                var cupon = await _context.Cupones
                    .FirstOrDefaultAsync(c => c.CodigoQR == codigoCuponLimpio && c.Activo);

                if (cupon == null)
                {
                    TempData["Error"] = "Cupón no encontrado o inactivo";
                    return RedirectToAction("EscanearQR");
                }

                // ... resto de validaciones existentes ...

                // Obtener productos aplicables
                var productos = await ObtenerProductosAplicables(cupon);

                if (!productos.Any())
                {
                    TempData["Error"] = "No hay productos disponibles para este cupón";
                    return RedirectToAction("EscanearQR");
                }

                // Pasar datos a la vista
                ViewBag.CuponEncontrado = cupon;
                ViewBag.ProductosDisponibles = productos;
                ViewBag.UsuarioQueEscanea = usuarioId; // ✅ PASAR USUARIO A LA VISTA

                return View("EscanearQR");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al procesar cupón: " + ex.Message;
                return RedirectToAction("EscanearQR");
            }
        }


        [HttpPost]
        [Authorize(Roles = "Administrador,Cajero")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AplicarCupon(int cuponId, Dictionary<int, ProductoSeleccionSimple> productos, string usuarioQueEscanea = null, string tipoServicioSeleccionado = null)
        {
            try
            {
                // ✅ OBTENER USUARIO QUE ESCANEÓ (desde TempData o parámetro)
                var usuarioDelCupon = usuarioQueEscanea ?? TempData["UsuarioQueEscanea"]?.ToString();
                Console.WriteLine($"[DEBUG] Aplicando cupón para usuario: {usuarioDelCupon}");
                Console.WriteLine($"[DEBUG] Tipo de servicio seleccionado: {tipoServicioSeleccionado}");

                var cupon = await _context.Cupones.FindAsync(cuponId);
                if (cupon == null)
                {
                    TempData["Error"] = "Cupón no encontrado";
                    return RedirectToAction("EscanearQR");
                }

                // ✅ VALIDAR QUE SE HAYA SELECCIONADO TIPO DE SERVICIO
                if (string.IsNullOrEmpty(tipoServicioSeleccionado))
                {
                    TempData["Error"] = "Debes seleccionar si es para 'Servir aquí' o 'Para llevar'";
                    return RedirectToAction("EscanearQR");
                }

                // Filtrar productos con cantidad > 0
                var productosSeleccionados = productos
                    .Where(p => p.Value.Cantidad > 0)
                    .ToList();

                if (!productosSeleccionados.Any())
                {
                    TempData["Error"] = "Selecciona al menos un producto";
                    return RedirectToAction("EscanearQR");
                }

                // Obtener productos de la BD
                var productosIds = productosSeleccionados.Select(p => p.Key).ToList();
                var productosDB = await _context.Productos
                    .Where(p => productosIds.Contains(p.Id))
                    .ToListAsync();

                // ✅ CREAR PEDIDO CON TIPO DE SERVICIO
                var pedido = await CrearPedidoParaUsuarioEspecificoConServicio(cupon, productosSeleccionados, productosDB, usuarioDelCupon, tipoServicioSeleccionado);

                // Marcar cupón como usado
                cupon.VecesUsado++;
                await _context.SaveChangesAsync();

                // ✅ GUARDAR INFO DE PUNTOS ANTES DE REDIRIGIR
                HttpContext.Session.SetString("CuponOtorgaPuntos", cupon.OtorgaPuntos.ToString());

                TempData["Success"] = "¡Cupón aplicado exitosamente!";

                // Redirección basada en el rol del usuario
                if (User.IsInRole("Administrador") || User.IsInRole("Cajero"))
                {
                    return RedirectToAction("ResumenAdmin", "Pedidos");
                }
                else
                {
                    return RedirectToAction("Resumen", "Pedidos", new { id = pedido.Id });
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error: " + ex.Message;
                return RedirectToAction("EscanearQR");
            }
        }

        // ✅ NUEVO MÉTODO QUE INCLUYE TIPO DE SERVICIO
        private async Task<Pedido> CrearPedidoParaUsuarioEspecificoConServicio(Cupon cupon, List<KeyValuePair<int, ProductoSeleccionSimple>> productosSeleccionados, List<Producto> productosDB, string usuarioDelCupon, string tipoServicio)
        {
            // Obtener sucursal
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

            // Calcular totales
            decimal totalOriginal = 0;
            var pedidoProductos = new List<PedidoProducto>();

            foreach (var item in productosSeleccionados)
            {
                var producto = productosDB.FirstOrDefault(p => p.Id == item.Key);
                if (producto != null)
                {
                    var cantidad = item.Value.Cantidad;
                    var subtotal = producto.Precio * cantidad;
                    totalOriginal += subtotal;

                    pedidoProductos.Add(new PedidoProducto
                    {
                        ProductoId = producto.Id,
                        Cantidad = cantidad,
                        Precio = producto.Precio
                    });
                }
            }

            // Calcular descuento
            decimal descuento = 0;
            switch (cupon.TipoDescuento)
            {
                case "3x2":
                    var cantidadTotal = productosSeleccionados.Sum(p => p.Value.Cantidad);
                    var gruposDe3 = cantidadTotal / 3;
                    var precioMasBarato = productosDB.Min(p => p.Precio);
                    descuento = gruposDe3 * precioMasBarato;
                    break;
                case "Porcentaje":
                    descuento = totalOriginal * (cupon.ValorDescuento / 100);
                    break;
                case "Fijo":
                    descuento = Math.Min(cupon.ValorDescuento, totalOriginal);
                    break;
            }

            var totalFinal = totalOriginal - descuento;

            // ✅ CREAR PEDIDO CON TIPO DE SERVICIO
            var pedido = new Pedido
            {
                Fecha = DateTime.Now,
                SucursalId = sucursal.Id,
                Estado = "Preparándose",
                Total = totalFinal,
                UsuarioId = usuarioDelCupon,
                PedidoProductos = pedidoProductos,
                EsCupon = true,
                TipoServicio = tipoServicio // ✅ AGREGAR TIPO DE SERVICIO
            };

            _context.Pedidos.Add(pedido);
            await _context.SaveChangesAsync();

            // ✅ AGREGAR PUNTOS SI EL CUPÓN OTORGA PUNTOS
            if (cupon.OtorgaPuntos && !string.IsNullOrEmpty(usuarioDelCupon))
            {
                Console.WriteLine($"[DEBUG] Cupón otorga puntos - agregando puntos por total: {totalFinal}");
                await AgregarPuntosAUsuario(usuarioDelCupon, totalFinal);
            }

            // Registrar cupón canjeado
            var cuponCanjeado = new CuponCanjeado
            {
                CuponId = cupon.Id,
                UsuarioId = usuarioDelCupon,
                CodigoQR = cupon.CodigoQR,
                FechaCanje = DateTime.Now,
                TotalOriginal = totalOriginal,
                DescuentoAplicado = descuento,
                TotalConDescuento = totalFinal,
                ProductosCanjeados = string.Join(",", pedidoProductos.Select(p => $"{p.ProductoId}:{p.Cantidad}")),
                PedidoId = pedido.Id
            };

            _context.CuponesCanjeados.Add(cuponCanjeado);
            await _context.SaveChangesAsync();

            Console.WriteLine($"[DEBUG] Pedido {pedido.Id} creado para usuario: {usuarioDelCupon} con servicio: {tipoServicio}");
            return pedido;
        }


        private async Task<Pedido> CrearPedidoParaUsuarioEspecifico(Cupon cupon, List<KeyValuePair<int, ProductoSeleccionSimple>> productosSeleccionados, List<Producto> productosDB, string usuarioDelCupon)
        {
            // Obtener sucursal
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

            // Calcular totales (código existente)
            decimal totalOriginal = 0;
            var pedidoProductos = new List<PedidoProducto>();

            foreach (var item in productosSeleccionados)
            {
                var producto = productosDB.FirstOrDefault(p => p.Id == item.Key);
                if (producto != null)
                {
                    var cantidad = item.Value.Cantidad;
                    var subtotal = producto.Precio * cantidad;
                    totalOriginal += subtotal;

                    pedidoProductos.Add(new PedidoProducto
                    {
                        ProductoId = producto.Id,
                        Cantidad = cantidad,
                        Precio = producto.Precio
                    });
                }
            }

            // Calcular descuento (código existente)
            decimal descuento = 0;
            switch (cupon.TipoDescuento)
            {
                case "3x2":
                    var cantidadTotal = productosSeleccionados.Sum(p => p.Value.Cantidad);
                    var gruposDe3 = cantidadTotal / 3;
                    var precioMasBarato = productosDB.Min(p => p.Precio);
                    descuento = gruposDe3 * precioMasBarato;
                    break;
                case "Porcentaje":
                    descuento = totalOriginal * (cupon.ValorDescuento / 100);
                    break;
                case "Fijo":
                    descuento = Math.Min(cupon.ValorDescuento, totalOriginal);
                    break;
            }

            var totalFinal = totalOriginal - descuento;

            // ✅ CREAR PEDIDO ASIGNADO AL USUARIO QUE ESCANEÓ
            var pedido = new Pedido
            {
                Fecha = DateTime.Now,
                SucursalId = sucursal.Id,
                Estado = "Preparándose",
                Total = totalFinal,
                UsuarioId = usuarioDelCupon,
                PedidoProductos = pedidoProductos,
                EsCupon = true
            };

            _context.Pedidos.Add(pedido);
            await _context.SaveChangesAsync();

            // ✅ AGREGAR PUNTOS SI EL CUPÓN OTORGA PUNTOS
            if (cupon.OtorgaPuntos && !string.IsNullOrEmpty(usuarioDelCupon))
            {
                Console.WriteLine($"[DEBUG] Cupón otorga puntos - agregando puntos por total: {totalFinal}");
                await AgregarPuntosAUsuario(usuarioDelCupon, totalFinal);
            }
            else
            {
                Console.WriteLine($"[DEBUG] Cupón NO otorga puntos o usuario nulo - OtorgaPuntos: {cupon.OtorgaPuntos}, UsuarioId: {usuarioDelCupon}");
            }

            // Registrar cupón canjeado
            var cuponCanjeado = new CuponCanjeado
            {
                CuponId = cupon.Id,
                UsuarioId = usuarioDelCupon,
                CodigoQR = cupon.CodigoQR,
                FechaCanje = DateTime.Now,
                TotalOriginal = totalOriginal,
                DescuentoAplicado = descuento,
                TotalConDescuento = totalFinal,
                ProductosCanjeados = string.Join(",", pedidoProductos.Select(p => $"{p.ProductoId}:{p.Cantidad}")),
                PedidoId = pedido.Id
            };

            _context.CuponesCanjeados.Add(cuponCanjeado);
            await _context.SaveChangesAsync();

            Console.WriteLine($"[DEBUG] Pedido {pedido.Id} creado para usuario: {usuarioDelCupon}");
            return pedido;
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
                    Descripcion = $"Puntos ganados por cupón - Total: ${totalPedido:F2}",
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

        // Crear pedido simple
        private async Task<Pedido> CrearPedidoSimple(Cupon cupon, List<KeyValuePair<int, ProductoSeleccionSimple>> productosSeleccionados, List<Producto> productosDB)
        {
            // Obtener sucursal
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

            // Calcular total
            decimal totalOriginal = 0;
            var pedidoProductos = new List<PedidoProducto>();

            foreach (var item in productosSeleccionados)
            {
                var producto = productosDB.FirstOrDefault(p => p.Id == item.Key);
                if (producto != null)
                {
                    var cantidad = item.Value.Cantidad;
                    var subtotal = producto.Precio * cantidad;
                    totalOriginal += subtotal;

                    pedidoProductos.Add(new PedidoProducto
                    {
                        ProductoId = producto.Id,
                        Cantidad = cantidad,
                        Precio = producto.Precio
                    });
                }
            }

            // Calcular descuento
            decimal descuento = 0;
            switch (cupon.TipoDescuento)
            {
                case "3x2":
                    var cantidadTotal = productosSeleccionados.Sum(p => p.Value.Cantidad);
                    var gruposDe3 = cantidadTotal / 3;
                    var precioMasBarato = productosDB.Min(p => p.Precio);
                    descuento = gruposDe3 * precioMasBarato;
                    break;
                case "Porcentaje":
                    descuento = totalOriginal * (cupon.ValorDescuento / 100);
                    break;
                case "Fijo":
                    descuento = Math.Min(cupon.ValorDescuento, totalOriginal);
                    break;
            }

            var totalFinal = totalOriginal - descuento;

            // Crear pedido
            var pedido = new Pedido
            {
                Fecha = DateTime.Now,
                SucursalId = sucursal.Id,
                Estado = "Preparándose",
                Total = totalFinal,
                UsuarioId = User.Identity.IsAuthenticated ? User.FindFirstValue(ClaimTypes.NameIdentifier) : null,
                PedidoProductos = pedidoProductos
            };

            _context.Pedidos.Add(pedido);
            await _context.SaveChangesAsync();

            // Registrar cupón canjeado
            var cuponCanjeado = new CuponCanjeado
            {
                CuponId = cupon.Id,
                UsuarioId = pedido.UsuarioId,
                CodigoQR = cupon.CodigoQR,
                FechaCanje = DateTime.Now,
                TotalOriginal = totalOriginal,
                DescuentoAplicado = descuento,
                TotalConDescuento = totalFinal,
                ProductosCanjeados = string.Join(",", pedidoProductos.Select(p => $"{p.ProductoId}:{p.Cantidad}")),
                PedidoId = pedido.Id
            };

            _context.CuponesCanjeados.Add(cuponCanjeado);
            await _context.SaveChangesAsync();

            return pedido;
        }

        // Métodos auxiliares
        private List<Cupon> FiltrarCuponesPorDia(List<Cupon> cupones)
        {
            var diaActual = DateTime.Now.DayOfWeek.ToString();
            return cupones.Where(c => CuponAplicaHoy(c)).ToList();
        }

        private bool CuponAplicaHoy(Cupon cupon)
        {
            if (string.IsNullOrEmpty(cupon.DiasAplicables))
                return true;

            // ✅ CONVERTIR DÍA ACTUAL A ESPAÑOL
            var diaActualIngles = DateTime.Now.DayOfWeek.ToString();
            var diaActualEspanol = ConvertirDiaAEspanol(diaActualIngles);

            var diasAplicables = cupon.DiasAplicables.Split(',').Select(d => d.Trim());

            return diasAplicables.Contains(diaActualEspanol) || diasAplicables.Contains("Todos");
        }
        private string ConvertirDiaAEspanol(string diaIngles)
        {
            return diaIngles switch
            {
                "Monday" => "Lunes",
                "Tuesday" => "Martes",
                "Wednesday" => "Miércoles",
                "Thursday" => "Jueves",
                "Friday" => "Viernes",
                "Saturday" => "Sábado",
                "Sunday" => "Domingo",
                _ => diaIngles
            };
        }

        private async Task<List<Producto>> ObtenerProductosAplicables(Cupon cupon)
        {
            if (string.IsNullOrEmpty(cupon.ProductosAplicables))
                return new List<Producto>();

            var productosIds = cupon.ProductosAplicables
                .Split(',')
                .Select(id => int.TryParse(id.Trim(), out int result) ? result : 0)
                .Where(id => id > 0)
                .ToList();

            return await _context.Productos
                .Where(p => productosIds.Contains(p.Id))
                .ToListAsync();
        }

        private async Task<Pedido> CrearPedidoConCuponSimple(Cupon cupon, List<PedidoProducto> productos, decimal totalOriginal, decimal descuento, decimal totalFinal)
        {
            // Obtener o crear sucursal
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

            // Crear pedido
            var pedido = new Pedido
            {
                Fecha = DateTime.Now,
                SucursalId = sucursal.Id,
                Estado = "Preparándose",
                Total = totalFinal,
                UsuarioId = User.Identity.IsAuthenticated ? User.FindFirstValue(ClaimTypes.NameIdentifier) : null,
                PedidoProductos = productos
            };

            // Asignar el pedido a cada producto
            foreach (var producto in productos)
            {
                producto.Pedido = pedido;
            }

            _context.Pedidos.Add(pedido);
            await _context.SaveChangesAsync();

            // Crear registro de cupón canjeado
            var cuponCanjeado = new CuponCanjeado
            {
                CuponId = cupon.Id,
                UsuarioId = pedido.UsuarioId,
                CodigoQR = cupon.CodigoQR,
                FechaCanje = DateTime.Now,
                TotalOriginal = totalOriginal,
                DescuentoAplicado = descuento,
                TotalConDescuento = totalFinal,
                ProductosCanjeados = string.Join(",", productos.Select(p => $"{p.ProductoId}:{p.Cantidad}")),
                PedidoId = pedido.Id
            };

            _context.CuponesCanjeados.Add(cuponCanjeado);
            await _context.SaveChangesAsync();

            return pedido;
        }

        // Clases auxiliares
        public class ProductoCuponSeleccionado
        {
            public int Id { get; set; }
            public string Nombre { get; set; }
            public decimal Precio { get; set; }
            public int Cantidad { get; set; }
        }

        public class CalculoDescuentoResult
        {
            public bool Success { get; set; } = true;
            public string Mensaje { get; set; } = "";
            public decimal TotalOriginal { get; set; }
            public decimal DescuentoAplicado { get; set; }
            public decimal TotalFinal { get; set; }
        }

        // Clase simple para recibir productos
        public class ProductoSeleccionSimple
        {
            public int Id { get; set; }
            public int Cantidad { get; set; }
            public decimal Precio { get; set; }
        }
    }
}
