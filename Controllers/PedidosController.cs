﻿using Microsoft.AspNetCore.Mvc;
using ProyectoIdentity.Datos;
using ProyectoIdentity.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ProyectoIdentity.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Newtonsoft.Json;

[Authorize]
public class PedidosController : Controller
{

    private readonly ApplicationDbContext _context;

    public PedidosController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<IActionResult> Crear(int productoId, int cantidad)
    {
        var producto = await _context.Productos.FindAsync(productoId);
        if (producto == null) return NotFound();

        // Obtenemos la sucursal seleccionada de la sesión
        var sucursalId = HttpContext.Session.GetInt32("SucursalSeleccionada");

        // Si no hay sucursal seleccionada, intentamos obtener la primera sucursal
        if (sucursalId == null)
        {
            var primeraSucursal = await _context.Sucursales.FirstOrDefaultAsync();
            if (primeraSucursal == null)
            {
                // Crear una sucursal si no existe ninguna (opcional)
                var nuevaSucursal = new Sucursal
                {
                    Nombre = "Verace Pizza",
                    Direccion = "Av. de los Shyris N35-52",
                    Latitud = -0.240653,
                    Longitud = -78.487834
                };
                _context.Sucursales.Add(nuevaSucursal);
                await _context.SaveChangesAsync();

                sucursalId = nuevaSucursal.Id;
            }
            else
            {
                sucursalId = primeraSucursal.Id;
            }

            // Guardamos la sucursal en la sesión
            HttpContext.Session.SetInt32("SucursalSeleccionada", sucursalId.Value);
        }

        var pedido = new Pedido
        {
            UsuarioId = User.Identity.IsAuthenticated ? User.FindFirstValue(ClaimTypes.NameIdentifier) : null,
            Fecha = DateTime.Now,
            SucursalId = sucursalId.Value, // Asignamos la sucursal al pedido
            PedidoProductos = new List<PedidoProducto>
            {
                new PedidoProducto
                {
                    ProductoId = producto.Id,
                    Cantidad = cantidad,
                    Precio = producto.Precio
                }
            }
        };

        _context.Pedidos.Add(pedido);
        await _context.SaveChangesAsync();

        return RedirectToAction("Resumen", new { id = pedido.Id });
    }

    public async Task<IActionResult> Resumen(int id)
    {
        var pedido = await _context.Pedidos
            .Include(p => p.PedidoProductos!)
                .ThenInclude(pp => pp.Producto)
            .Include(p => p.Sucursal)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (pedido == null)
            return NotFound();

        // ✅ VALIDACIÓN DE SEGURIDAD: Solo el propietario del pedido puede verlo
        if (User.Identity.IsAuthenticated)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Si el pedido tiene un usuario asignado y no es el usuario actual, denegar acceso
            if (!string.IsNullOrEmpty(pedido.UsuarioId) && pedido.UsuarioId != currentUserId)
            {
                return Forbid(); // Devuelve 403 Forbidden
            }
        }
        else
        {
            // Si el usuario no está autenticado y el pedido tiene un usuario asignado, denegar acceso
            if (!string.IsNullOrEmpty(pedido.UsuarioId))
            {
                return Forbid();
            }
        }

        // Si es cupón, verificar si otorga puntos
        if (pedido.EsCupon == true)
        {
            var cuponCanjeado = await _context.CuponesCanjeados
                .Include(cc => cc.Cupon)
                .FirstOrDefaultAsync(cc => cc.PedidoId == pedido.Id);

            ViewBag.CuponOtorgaPuntos = cuponCanjeado?.Cupon?.OtorgaPuntos ?? false;
        }
        // Verificar también en sesión
        if (HttpContext.Session.GetString("CuponOtorgaPuntos") == "True")
        {
            ViewBag.CuponOtorgaPuntos = true;
            // Limpiar sesión después de usar
            HttpContext.Session.Remove("CuponOtorgaPuntos");
        }

        return View(pedido);
    }

    public async Task<IActionResult> Admin()
    {
        var pedidos = await _context.Pedidos
            .Include(p => p.Sucursal) // Incluir la sucursal relacionada
            .ToListAsync();
        return View(pedidos);
    }

    [HttpGet]
    public async Task<IActionResult> SeleccionarProductos()
    {
        var productos = await _context.Productos.ToListAsync();

        var viewModel = new PedidoViewModel
        {
            ProductosSeleccionados = productos.Select(p => new ProductoSeleccionado
            {
                ProductoId = p.Id,
                Nombre = p.Nombre,
                Precio = p.Precio,
                Cantidad = 1
            }).ToList()
        };

        return View(viewModel);
    }

    [HttpPost]
    public async Task<IActionResult> CrearPedidoMultiple(PedidoViewModel model)
    {
        var productosElegidos = model.ProductosSeleccionados
            .Where(p => p.Cantidad > 0)
            .ToList();

        if (!productosElegidos.Any()) return RedirectToAction("SeleccionarProductos");

        // Obtenemos la sucursal seleccionada de la sesión
        var sucursalId = HttpContext.Session.GetInt32("SucursalSeleccionada");

        // Si no hay sucursal seleccionada, intentamos obtener la primera sucursal
        if (sucursalId == null)
        {
            var primeraSucursal = await _context.Sucursales.FirstOrDefaultAsync();
            if (primeraSucursal != null)
            {
                sucursalId = primeraSucursal.Id;
                HttpContext.Session.SetInt32("SucursalSeleccionada", sucursalId.Value);
            }
            else
            {
                return RedirectToAction("SeleccionarSucursal", new { returnUrl = Request.Path });
            }
        }

        var pedido = new Pedido
        {
            UsuarioId = User.Identity.IsAuthenticated ? User.FindFirstValue(ClaimTypes.NameIdentifier) : null,
            Fecha = DateTime.Now,
            SucursalId = sucursalId.Value,
            PedidoProductos = productosElegidos.Select(p => new PedidoProducto
            {
                ProductoId = p.ProductoId,
                Cantidad = p.Cantidad,
                Precio = p.Precio
            }).ToList()
        };

        _context.Pedidos.Add(pedido);
        await _context.SaveChangesAsync();

        return RedirectToAction("Resumen", new { id = pedido.Id });
    }



    // REEMPLAZA tu método AgregarSeleccionados con este:

    [HttpPost]
    public async Task<IActionResult> AgregarSeleccionados(List<ProductoSeleccionadoInput> seleccionados)
    {
        // ✅ AGREGAR LOGS DE DEBUG
        Console.WriteLine($"[DEBUG] AgregarSeleccionados - Usuario autenticado: {User.Identity.IsAuthenticated}");
        if (User.Identity.IsAuthenticated)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            Console.WriteLine($"[DEBUG] AgregarSeleccionados - UsuarioId: {userId}");
        }

        var seleccionadosValidos = seleccionados
            .Where(p => p.Seleccionado && p.Cantidad > 0)
            .ToList();

        if (!seleccionadosValidos.Any())
        {
            return BadRequest("Datos inválidos");
        }

        // Obtener la primera sucursal directamente de la base de datos
        var sucursal = await _context.Sucursales.FirstOrDefaultAsync();
        if (sucursal == null)
        {
            // Crear una sucursal si no existe ninguna
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

        var pedido = new Pedido
        {
            Fecha = DateTime.Now,
            SucursalId = sucursal.Id,
            PedidoProductos = new List<PedidoProducto>(),
            UsuarioId = User.Identity.IsAuthenticated ? User.FindFirstValue(ClaimTypes.NameIdentifier) : null // ✅ AGREGAR ESTA LÍNEA
        };

        Console.WriteLine($"[DEBUG] AgregarSeleccionados - Pedido creado con UsuarioId: {pedido.UsuarioId}");

        decimal total = 0;

        foreach (var item in seleccionadosValidos)
        {
            var producto = await _context.Productos.FindAsync(item.ProductoId);
            if (producto != null)
            {
                decimal subtotal = producto.Precio * item.Cantidad;
                total += subtotal;

                pedido.PedidoProductos.Add(new PedidoProducto
                {
                    ProductoId = producto.Id,
                    Cantidad = item.Cantidad,
                    Precio = producto.Precio
                });
            }
        }

        pedido.Total = total;

        _context.Pedidos.Add(pedido);
        await _context.SaveChangesAsync();

        Console.WriteLine($"[DEBUG] AgregarSeleccionados - Pedido guardado. Total: {total}");

        // ✅ AGREGAR PUNTOS AL USUARIO SI ESTÁ AUTENTICADO
        if (User.Identity.IsAuthenticated && total > 0)
        {
            Console.WriteLine($"[DEBUG] AgregarSeleccionados - Llamando AgregarPuntosAUsuario");
            await AgregarPuntosAUsuario(pedido.UsuarioId, total);
        }
        else
        {
            Console.WriteLine($"[DEBUG] AgregarSeleccionados - NO se agregaron puntos - Autenticado: {User.Identity.IsAuthenticated}, Total: {total}");
        }

        // Guardar ID del pedido en una cookie por 30 minutos
        CookieOptions options = new CookieOptions
        {
            Expires = DateTimeOffset.Now.AddMinutes(30)
        };
        Response.Cookies.Append("PedidoTemporalId", pedido.Id.ToString(), options);

        return RedirectToAction("Seleccionar", "Recoleccion");
    }

    [Authorize(Roles = "Administrador,Cajero")]
    public async Task<IActionResult> ResumenAdmin()
    {
        try
        {
            Console.WriteLine("[DEBUG] ResumenAdmin - Iniciando consulta...");

            // ✅ CONSULTA SEGURA SIN INCLUDES PROBLEMÁTICOS
            var pedidos = await _context.Pedidos
                .AsNoTracking()
                .Include(p => p.Sucursal)
                .OrderByDescending(p => p.Fecha)
                .ToListAsync();

            Console.WriteLine($"[DEBUG] ResumenAdmin - {pedidos.Count} pedidos encontrados");

            // ✅ CARGAR LAS RELACIONES POR SEPARADO PARA EVITAR CONFLICTOS
            foreach (var pedido in pedidos)
            {
                // Cargar PedidoProductos
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
            }

            Console.WriteLine("[DEBUG] ResumenAdmin - Enviando a vista...");
            return View(pedidos);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] ResumenAdmin: {ex.Message}");
            Console.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");

            TempData["Error"] = "Error al cargar los pedidos: " + ex.Message;

            // ✅ FALLBACK: Versión súper simple
            try
            {
                var pedidosSimple = await _context.Pedidos
                    .AsNoTracking()
                    .OrderByDescending(p => p.Fecha)
                    .Take(10) // Solo los últimos 10
                    .ToListAsync();

                Console.WriteLine($"[DEBUG] Fallback - {pedidosSimple.Count} pedidos cargados");
                return View(pedidosSimple);
            }
            catch (Exception ex2)
            {
                Console.WriteLine($"[ERROR] Fallback también falló: {ex2.Message}");
                return View(new List<Pedido>());
            }
        }
    }

    // ViewModel para mostrar pedidos con información de cupones
    public class PedidoResumenViewModel
    {
        public int Id { get; set; }
        public DateTime Fecha { get; set; }
        public string Estado { get; set; }
        public decimal Total { get; set; }
        public string SucursalNombre { get; set; }
        public string UsuarioId { get; set; }
        public bool UsoCupon { get; set; }
        public dynamic CuponInfo { get; set; }
        public int ProductosCount { get; set; }
    }

    // EN PedidosController.cs - REEMPLAZAR el método VerPedidoTemporal

    // MÉTODO VERPEDIDOTEMPORAL CORREGIDO - Solo busca pedidos del usuario actual
    public async Task<IActionResult> VerPedidoTemporal()
    {
        Console.WriteLine($"[DEBUG] VerPedidoTemporal - Usuario autenticado: {User.Identity.IsAuthenticated}");

        Pedido pedido = null;

        // ✅ SI EL USUARIO ESTÁ AUTENTICADO, SOLO BUSCAR SUS PEDIDOS
        if (User.Identity.IsAuthenticated)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            Console.WriteLine($"[DEBUG] Buscando último pedido para usuario: {userId}");

            // Buscar SOLO pedidos del usuario autenticado
            pedido = await _context.Pedidos
                .Include(p => p.PedidoProductos)
                    .ThenInclude(pp => pp.Producto)
                .Include(p => p.Sucursal)
                .Where(p => p.UsuarioId == userId) // ✅ FILTRAR SOLO POR USUARIO ACTUAL
                .OrderByDescending(p => p.Fecha)
                .FirstOrDefaultAsync();

            if (pedido != null)
            {
                Console.WriteLine($"[DEBUG] Pedido encontrado para usuario autenticado: {pedido.Id}");
                return RedirectToAction("Resumen", new { id = pedido.Id });
            }
            else
            {
                Console.WriteLine($"[DEBUG] No se encontraron pedidos para el usuario: {userId}");
                TempData["Mensaje"] = "No tienes pedidos recientes";
                return RedirectToAction("Index", "Home");
            }
        }

        // ✅ SI NO HAY USUARIO AUTENTICADO, BUSCAR SOLO PEDIDOS SIN USUARIO ASIGNADO
        int? pedidoId = null;

        // 1. Intentar obtener el ID del pedido de la sesión
        pedidoId = HttpContext.Session.GetInt32("PedidoActualId");
        Console.WriteLine($"[DEBUG] ID de sesión: {pedidoId}");

        // 2. Si no está en la sesión, intentar obtenerlo de la cookie
        if (!pedidoId.HasValue && Request.Cookies.TryGetValue("PedidoTemporalId", out string pedidoTemporalIdStr))
        {
            if (int.TryParse(pedidoTemporalIdStr, out int id))
            {
                pedidoId = id;
                Console.WriteLine($"[DEBUG] ID de cookie: {pedidoId}");
            }
        }

        // Si encontramos un ID de pedido, intentar cargar el pedido
        if (pedidoId.HasValue)
        {
            pedido = await _context.Pedidos
                .Include(p => p.PedidoProductos)
                    .ThenInclude(pp => pp.Producto)
                .Include(p => p.Sucursal)
                .Where(p => p.Id == pedidoId.Value && p.UsuarioId == null) // ✅ SOLO PEDIDOS SIN USUARIO
                .FirstOrDefaultAsync();

            if (pedido != null)
            {
                Console.WriteLine($"[DEBUG] Pedido encontrado por ID: {pedido.Id}");
                return RedirectToAction("Resumen", new { id = pedido.Id });
            }
        }

        // Si no hay pedidos, redirigir al inicio
        Console.WriteLine("[DEBUG] No se encontró ningún pedido");
        TempData["Mensaje"] = "No se encontró ningún pedido reciente";
        return RedirectToAction("Index", "Home");
    }
    [HttpPost]
    public async Task<IActionResult> ActualizarEstado(int id, string estado)
    {
        var pedido = await _context.Pedidos.FindAsync(id);
        if (pedido == null)
        {
            return NotFound();
        }

        pedido.Estado = estado;
        await _context.SaveChangesAsync();

        return RedirectToAction("ResumenAdmin");
    }

    public async Task<IActionResult> SeleccionarSucursal(double lat, double lng, string returnUrl = null)
    {
        var sucursales = await _context.Sucursales.ToListAsync();

        if (!sucursales.Any())
        {
            // Si no hay sucursales, crear una por defecto
            await CrearSucursalesPrueba();
            sucursales = await _context.Sucursales.ToListAsync();
        }

        ViewBag.UserLat = lat;
        ViewBag.UserLng = lng;
        ViewBag.ReturnUrl = returnUrl;

        return View(sucursales);
    }

    [HttpPost]
    public async Task<IActionResult> ConfirmarSucursal(int sucursalId, double lat, double lng, string returnUrl = null)
    {
        var sucursal = await _context.Sucursales.FindAsync(sucursalId);
        if (sucursal == null) return NotFound();

        HttpContext.Session.SetInt32("SucursalSeleccionada", sucursalId);

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return View("ConfirmarSucursal", sucursal);
    }

    public async Task<IActionResult> ResumenPedido()
    {
        // Recuperar los datos serializados
        var productosJson = TempData["ProductosSeleccionados"] as string;

        if (string.IsNullOrEmpty(productosJson))
        {
            return RedirectToAction("SeleccionMultiple", "Productos");
        }

        // Deserializar los datos
        var elementosCarrito = JsonConvert.DeserializeObject<List<ElementoCarrito>>(productosJson);

        // Obtener los IDs de productos
        var productosIds = elementosCarrito.Select(e => e.ProductoId).ToList();

        // Buscar los datos completos de los productos en la base de datos
        var productos = await _context.Productos
            .Where(p => productosIds.Contains(p.Id))
            .ToListAsync();

        // Crear un nuevo pedido
        var pedido = new Pedido
        {
            Fecha = DateTime.Now,
            Estado = "Pendiente",
            PedidoProductos = new List<PedidoProducto>()
        };

        // Obtener el usuario actual si está autenticado
        if (User.Identity.IsAuthenticated)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            pedido.UsuarioId = userId;
        }

        // Crear los PedidoProductos
        decimal totalCalculado = 0;
        foreach (var producto in productos)
        {
            var elemento = elementosCarrito.First(e => e.ProductoId == producto.Id);
            var subtotal = producto.Precio * elemento.Cantidad;
            totalCalculado += subtotal;

            pedido.PedidoProductos.Add(new PedidoProducto
            {
                ProductoId = producto.Id,
                Producto = producto,
                Cantidad = elemento.Cantidad,
                Precio = producto.Precio
            });
        }

        // Asignar el total calculado
        pedido.Total = totalCalculado;

        // Cargar las sucursales para el dropdown
        ViewBag.Sucursales = await _context.Sucursales.ToListAsync();

        // Guardar en TempData para procesamiento posterior
        TempData["PedidoPendiente"] = JsonConvert.SerializeObject(new
        {
            ProductosIds = string.Join(",", elementosCarrito.Select(e => e.ProductoId)),
            Cantidades = string.Join(",", elementosCarrito.Select(e => e.Cantidad))
        });

        // Preservar los productos seleccionados
        TempData.Keep("ProductosSeleccionados");

        return View(pedido);
    }

    [HttpPost]
    public async Task<IActionResult> ConfirmarPedido(int? sucursalId, string direccionEntrega = null)
    {
        Console.WriteLine($"[DEBUG] ConfirmarPedido - Usuario autenticado: {User.Identity.IsAuthenticated}");

        var pedidoJson = TempData["PedidoPendiente"] as string;
        if (string.IsNullOrEmpty(pedidoJson))
        {
            return RedirectToAction("SeleccionMultiple", "Productos");
        }

        var productosJson = TempData["ProductosSeleccionados"] as string;
        var elementosCarrito = JsonConvert.DeserializeObject<List<ElementoCarrito>>(productosJson);

        var pedidoData = JsonConvert.DeserializeObject<dynamic>(pedidoJson);
        var productosIds = ((string)pedidoData.ProductosIds).Split(',').Select(int.Parse).ToList();
        var cantidades = ((string)pedidoData.Cantidades).Split(',').Select(int.Parse).ToList();

        // Verificar si tenemos una sucursal válida, si no, buscar una por defecto
        int sucursalIdValor;
        if (sucursalId.HasValue && sucursalId.Value > 0)
        {
            sucursalIdValor = sucursalId.Value;
        }
        else
        {
            // Buscar una sucursal por defecto
            var sucursalPorDefecto = await _context.Sucursales.FirstOrDefaultAsync();
            if (sucursalPorDefecto == null)
            {
                // Crear una sucursal si no existe ninguna
                var nuevaSucursal = new Sucursal
                {
                    Nombre = "Verace Pizza",
                    Direccion = "Av. de los Shyris N35-52",
                    Latitud = -0.180653,
                    Longitud = -78.487834
                };
                _context.Sucursales.Add(nuevaSucursal);
                await _context.SaveChangesAsync();
                sucursalIdValor = nuevaSucursal.Id;
            }
            else
            {
                sucursalIdValor = sucursalPorDefecto.Id;
            }
        }

        // Crear nuevo pedido con la sucursal válida
        var pedido = new Pedido
        {
            Fecha = DateTime.Now,
            Estado = "Pendiente",
            SucursalId = sucursalIdValor,
            PedidoProductos = new List<PedidoProducto>(),
            UsuarioId = User.Identity.IsAuthenticated ? User.FindFirstValue(ClaimTypes.NameIdentifier) : null // ✅ AGREGAR ESTA LÍNEA
        };

        Console.WriteLine($"[DEBUG] ConfirmarPedido - Pedido creado con UsuarioId: {pedido.UsuarioId}");

        // Agregar productos al pedido
        decimal totalCalculado = 0;
        for (int i = 0; i < productosIds.Count; i++)
        {
            var productoId = productosIds[i];
            var cantidad = cantidades[i];

            var producto = await _context.Productos.FindAsync(productoId);
            if (producto != null)
            {
                var subtotal = producto.Precio * cantidad;
                totalCalculado += subtotal;

                pedido.PedidoProductos.Add(new PedidoProducto
                {
                    ProductoId = productoId,
                    Cantidad = cantidad,
                    Precio = producto.Precio
                });
            }
        }

        // Asignar el total calculado
        pedido.Total = totalCalculado;

        // Guardar en la base de datos
        _context.Pedidos.Add(pedido);
        await _context.SaveChangesAsync();

        Console.WriteLine($"[DEBUG] ConfirmarPedido - Pedido guardado. Total: {totalCalculado}");

        // ✅ AGREGAR PUNTOS AL USUARIO SI ESTÁ AUTENTICADO
        if (User.Identity.IsAuthenticated && totalCalculado > 0)
        {
            Console.WriteLine($"[DEBUG] ConfirmarPedido - Llamando AgregarPuntosAUsuario");
            await AgregarPuntosAUsuario(pedido.UsuarioId, totalCalculado);
        }
        else
        {
            Console.WriteLine($"[DEBUG] ConfirmarPedido - NO se agregaron puntos - Autenticado: {User.Identity.IsAuthenticated}, Total: {totalCalculado}");
        }

        // Redirigir a la página de confirmación
        return RedirectToAction("Resumen", new { id = pedido.Id });
    }

    // EN PedidosController.cs - CORREGIR el método DetallePedido

    // MÉTODO DETALLEPEDIDO CORREGIDO - Con validación de usuario
    public async Task<IActionResult> DetallePedido(int id)
    {
        var pedido = await _context.Pedidos
            .Include(p => p.PedidoProductos)
            .ThenInclude(pp => pp.Producto)
            .Include(p => p.Sucursal)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (pedido == null)
        {
            return NotFound();
        }

        // ✅ VALIDACIÓN DE SEGURIDAD: Solo el propietario del pedido puede verlo
        if (User.Identity.IsAuthenticated)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Si el pedido tiene un usuario asignado y no es el usuario actual, denegar acceso
            if (!string.IsNullOrEmpty(pedido.UsuarioId) && pedido.UsuarioId != currentUserId)
            {
                return Forbid();
            }
        }
        else
        {
            // Si el usuario no está autenticado y el pedido tiene un usuario asignado, denegar acceso
            if (!string.IsNullOrEmpty(pedido.UsuarioId))
            {
                return Forbid();
            }
        }

        return View("Resumen", pedido);
    }

    // NUEVO MÉTODO: Listar pedidos del usuario actual
    [Authorize]
    public async Task<IActionResult> MisPedidos()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var pedidos = await _context.Pedidos
            .Include(p => p.Sucursal)
            .Include(p => p.PedidoProductos)
                .ThenInclude(pp => pp.Producto)
            .Where(p => p.UsuarioId == userId) // ✅ SOLO PEDIDOS DEL USUARIO ACTUAL
            .OrderByDescending(p => p.Fecha)
            .ToListAsync();

        return View(pedidos);
    }
    public async Task<IActionResult> SucursalMasCercana(double lat, double lng)
    {
        var sucursales = await _context.Sucursales.ToListAsync();

        if (!sucursales.Any())
        {
            // Si no hay sucursales, crear una por defecto
            await CrearSucursalesPrueba();
            sucursales = await _context.Sucursales.ToListAsync();
        }

        Sucursal? sucursalMasCercana = null;
        double menorDistancia = double.MaxValue;

        foreach (var sucursal in sucursales)
        {
            double distancia = CalcularDistancia(lat, lng, sucursal.Latitud, sucursal.Longitud);
            if (distancia < menorDistancia)
            {
                menorDistancia = distancia;
                sucursalMasCercana = sucursal;
            }
        }

        if (sucursalMasCercana == null)
        {
            return NotFound("No se encontraron sucursales.");
        }

        HttpContext.Session.SetInt32("SucursalSeleccionada", sucursalMasCercana.Id);

        return View("ConfirmarSucursal", sucursalMasCercana);
    }

    private double CalcularDistancia(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371; // Radio de la Tierra en kilómetros

        double dLat = GradosARadianes(lat2 - lat1);
        double dLon = GradosARadianes(lon2 - lon1);

        double a =
            Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
            Math.Cos(GradosARadianes(lat1)) * Math.Cos(GradosARadianes(lat2)) *
            Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return R * c;
    }

    private double GradosARadianes(double grados)
    {
        return grados * (Math.PI / 180);
    }

    public async Task<IActionResult> CrearSucursalesPrueba()
    {
        if (!await _context.Sucursales.AnyAsync())
        {
            _context.Sucursales.AddRange(
                new Sucursal { Nombre = "Verace Pizza", Direccion = "Av. de los Shyris N35-52", Latitud = -0.180653, Longitud = -78.467834 }
            );
            await _context.SaveChangesAsync();
        }

        return RedirectToAction("Index", "Home");
    }

    [HttpPost("crear-con-ubicacion")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearPedidoConUbicacion([FromBody] PedidoConUbicacionDTO datos)
    {
        try
        {
            // Primero verificamos si hay sucursales en la base de datos
            var existenSucursales = await _context.Sucursales.AnyAsync();
            if (!existenSucursales)
            {
                // Si no hay sucursales, creamos una predeterminada
                _context.Sucursales.Add(new Sucursal
                {
                    Nombre = "Verace Pizza",
                    Direccion = "Av. de los Shyris N35-52",
                    Latitud = -0.180653,
                    Longitud = -78.467834
                });
                await _context.SaveChangesAsync();
            }

            // Buscamos la sucursal más cercana
            var sucursal = await _context.Sucursales
                .OrderBy(s => Math.Sqrt(Math.Pow(s.Latitud - datos.Latitud, 2) + Math.Pow(s.Longitud - datos.Longitud, 2)))
                .FirstOrDefaultAsync();

            if (sucursal == null)
                return BadRequest(new { error = "No se pudo determinar una sucursal cercana" });

            // Verificamos que los productos existan
            if (datos.ProductosIdsSeleccionados == null || !datos.ProductosIdsSeleccionados.Any())
                return BadRequest(new { error = "No se seleccionaron productos" });

            var productos = await _context.Productos
                .Where(p => datos.ProductosIdsSeleccionados.Contains(p.Id))
                .ToListAsync();

            if (!productos.Any())
                return BadRequest(new { error = "Ninguno de los productos seleccionados existe" });

            // Creamos el pedido
            var pedido = new Pedido
            {
                UsuarioId = User.Identity.IsAuthenticated ? User.FindFirstValue(ClaimTypes.NameIdentifier) : null,
                Fecha = DateTime.Now,
                SucursalId = sucursal.Id,
                PedidoProductos = new List<PedidoProducto>()
            };

            // Calculamos el total a mano
            decimal totalCalculado = 0;

            // Agregamos los productos al pedido
            foreach (var producto in productos)
            {
                totalCalculado += producto.Precio;
                pedido.PedidoProductos.Add(new PedidoProducto
                {
                    ProductoId = producto.Id,
                    Cantidad = 1,
                    Precio = producto.Precio
                });
            }

            // Asignamos el total calculado
            pedido.Total = totalCalculado;

            // Guardamos en sesión la sucursal seleccionada
            HttpContext.Session.SetInt32("SucursalSeleccionada", sucursal.Id);

            _context.Pedidos.Add(pedido);
            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Pedido creado exitosamente", id = pedido.Id });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = $"Error al guardar el pedido: {ex.InnerException?.Message ?? ex.Message}" });
        }
    }

    // MÉTODO GUARDARCOMENTARIO CORREGIDO - Con validación de usuario
    [HttpPost]
    public async Task<IActionResult> GuardarComentario(int pedidoId, int calificacion, string comentario)
    {
        try
        {
            var pedido = await _context.Pedidos
                .Include(p => p.Detalles) // ✅ Incluir detalles para verificar si es personalización
                .FirstOrDefaultAsync(p => p.Id == pedidoId);

            if (pedido == null)
            {
                TempData["Error"] = "Pedido no encontrado";
                return NotFound();
            }

            // ✅ VALIDACIÓN DE SEGURIDAD: Solo el propietario del pedido puede comentar
            if (User.Identity.IsAuthenticated)
            {
                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!string.IsNullOrEmpty(pedido.UsuarioId) && pedido.UsuarioId != currentUserId)
                {
                    return Forbid();
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(pedido.UsuarioId))
                {
                    return Forbid();
                }
            }

            // Verificar que el pedido esté en estado "Entregado"
            if (pedido.Estado != "Entregado")
            {
                TempData["Error"] = "Solo se pueden comentar pedidos entregados";
                return BadRequest("Solo se pueden comentar pedidos entregados");
            }

            // Verificar que no haya comentado ya
            if (pedido.ComentarioEnviado)
            {
                TempData["Info"] = "Ya has enviado tu valoración para este pedido";

                // ✅ REDIRECCIÓN INTELIGENTE - mismo que abajo
                bool esPersonalizacion = pedido.Detalles != null && pedido.Detalles.Any();

                if (esPersonalizacion)
                {
                    return RedirectToAction("Confirmacion", "Personalizacion", new { id = pedidoId });
                }
                else
                {
                    return RedirectToAction("Resumen", new { id = pedidoId });
                }
            }

            // Validar calificación
            if (calificacion < 1 || calificacion > 5)
            {
                TempData["Error"] = "La calificación debe estar entre 1 y 5 estrellas";

                // Redirigir de vuelta sin guardar
                bool esPersonalizacion = pedido.Detalles != null && pedido.Detalles.Any();

                if (esPersonalizacion)
                {
                    return RedirectToAction("Confirmacion", "Personalizacion", new { id = pedidoId });
                }
                else
                {
                    return RedirectToAction("Resumen", new { id = pedidoId });
                }
            }

            // Validar comentario
            if (string.IsNullOrWhiteSpace(comentario))
            {
                TempData["Error"] = "El comentario es obligatorio";

                // Redirigir de vuelta sin guardar
                bool esPersonalizacion = pedido.Detalles != null && pedido.Detalles.Any();

                if (esPersonalizacion)
                {
                    return RedirectToAction("Confirmacion", "Personalizacion", new { id = pedidoId });
                }
                else
                {
                    return RedirectToAction("Resumen", new { id = pedidoId });
                }
            }

            // ✅ ASIGNAR COMENTARIO Y CALIFICACIÓN
            pedido.Comentario = comentario.Trim();
            pedido.Calificacion = calificacion;
            pedido.ComentarioEnviado = true;

            await _context.SaveChangesAsync();

            Console.WriteLine($"[DEBUG] Comentario guardado para pedido {pedidoId} - Calificación: {calificacion}");

            TempData["Success"] = "¡Gracias por tu valoración! Tu opinión es muy importante para nosotros.";

            // ✅ REDIRECCIÓN INTELIGENTE SEGÚN EL TIPO DE PEDIDO
            bool esPedidoPersonalizacion = pedido.Detalles != null && pedido.Detalles.Any();

            if (esPedidoPersonalizacion)
            {
                // Es un pedido de personalización → Redirigir a Personalizacion/Confirmacion
                Console.WriteLine($"[DEBUG] Redirigiendo a Personalizacion/Confirmacion para pedido {pedidoId}");
                return RedirectToAction("Confirmacion", "Personalizacion", new { id = pedidoId });
            }
            else
            {
                // Es un pedido normal → Redirigir a Pedidos/Resumen
                Console.WriteLine($"[DEBUG] Redirigiendo a Pedidos/Resumen para pedido {pedidoId}");
                return RedirectToAction("Resumen", new { id = pedidoId });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] GuardarComentario: {ex.Message}");
            TempData["Error"] = "Error al guardar el comentario. Inténtalo de nuevo.";

            // En caso de error, intentar redireccionar de forma segura
            return RedirectToAction("Index", "Home");
        }
    }

    // ✅ MÉTODO AUXILIAR: Determinar si un pedido es de personalización
    private async Task<bool> EsPedidoPersonalizacion(int pedidoId)
    {
        return await _context.PedidoDetalles.AnyAsync(d => d.PedidoId == pedidoId);
    }

    // CORRECCIÓN PARA LÍNEA 524 - Error CS0266: Cannot implicitly convert type 'int?' to 'int'
    // Método CreateOrder - Verificación de CollectionPointId

    [HttpPost]
    [Route("api/orders")]
    public async Task<IActionResult> CreateOrder([FromBody] OrderRequest request)
    {
        if (request == null || request.Cart == null || request.Cart.Count == 0)
        {
            return BadRequest("No se especificaron productos para el pedido");
        }

        if (request.CollectionPointId <= 0)
        {
            return BadRequest("No se especificó un punto de recolección válido");
        }

        // Crear el pedido
        var pedido = new Pedido
        {
            Fecha = DateTime.Now,
            Estado = "Preparándose",
            PedidoProductos = new List<PedidoProducto>(),
            UsuarioId = User.Identity.IsAuthenticated ? User.FindFirstValue(ClaimTypes.NameIdentifier) : null
        };

        // Buscar punto de recolección
        var puntoRecoleccion = await _context.CollectionPoints.FindAsync(request.CollectionPointId);
        if (puntoRecoleccion == null)
        {
            return NotFound("Punto de recolección no encontrado");
        }

        // Asignar sucursal
        if (puntoRecoleccion.SucursalId > 0)
        {
            pedido.SucursalId = puntoRecoleccion.SucursalId;
        }
        else
        {
            var sucursal = await _context.Sucursales.FirstOrDefaultAsync();
            if (sucursal == null)
            {
                return BadRequest("No hay sucursales disponibles para asignar al pedido");
            }
            pedido.SucursalId = sucursal.Id;
        }

        // Agregar productos al pedido
        decimal total = 0;
        foreach (var item in request.Cart)
        {
            var producto = await _context.Productos.FindAsync(item.ProductoId);
            if (producto != null)
            {
                var pedidoProducto = new PedidoProducto
                {
                    ProductoId = producto.Id,
                    Cantidad = item.Cantidad,
                    Precio = producto.Precio
                };

                pedido.PedidoProductos.Add(pedidoProducto);
                total += producto.Precio * item.Cantidad;
            }
        }

        pedido.Total = total;

        // Guardar en la base de datos
        _context.Pedidos.Add(pedido);
        await _context.SaveChangesAsync();

        // AGREGAR PUNTOS AL USUARIO SI ESTÁ AUTENTICADO
        if (User.Identity.IsAuthenticated && total > 0)
        {
            await AgregarPuntosAUsuario(pedido.UsuarioId, total);
        }

        return Ok(new { id = pedido.Id });
    }

    [HttpPost]
    public async Task<IActionResult> CrearPedidoAPI([FromBody] List<PedidoDetalle> detalles)
    {
        if (detalles == null || !detalles.Any())
            return BadRequest("Carrito vacío");

        var sucursalId = HttpContext.Session.GetInt32("SucursalSeleccionada");

        if (sucursalId == null)
        {
            var primeraSucursal = await _context.Sucursales.FirstOrDefaultAsync();
            if (primeraSucursal != null)
            {
                sucursalId = primeraSucursal.Id;
                HttpContext.Session.SetInt32("SucursalSeleccionada", sucursalId.Value);
            }
            else
            {
                return BadRequest("No se ha seleccionado una sucursal y no hay sucursales disponibles.");
            }
        }

        var pedido = new Pedido
        {
            Fecha = DateTime.Now,
            SucursalId = sucursalId.Value,
            PedidoProductos = new List<PedidoProducto>(),
            UsuarioId = User.Identity.IsAuthenticated ? User.FindFirstValue(ClaimTypes.NameIdentifier) : null
        };

        decimal totalCalculado = 0;
        foreach (var detalle in detalles)
        {
            var producto = await _context.Productos.FindAsync(detalle.ProductoId);
            if (producto != null)
            {
                decimal subtotal = detalle.Cantidad * detalle.PrecioUnitario;
                totalCalculado += subtotal;

                pedido.PedidoProductos.Add(new PedidoProducto
                {
                    ProductoId = detalle.ProductoId,
                    Cantidad = detalle.Cantidad,
                    Precio = detalle.PrecioUnitario
                });
            }
        }

        pedido.Total = totalCalculado;
        _context.Pedidos.Add(pedido);
        await _context.SaveChangesAsync();

        // AGREGAR PUNTOS AL USUARIO SI ESTÁ AUTENTICADO
        if (User.Identity.IsAuthenticated && totalCalculado > 0)
        {
            await AgregarPuntosAUsuario(pedido.UsuarioId, totalCalculado);
        }

        return Ok(new { mensaje = "Pedido creado exitosamente", pedido.Id });
    }

    // Actualizar ProcesarSeleccionMultiple
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ProcesarSeleccionMultiple(List<ProductoSeleccionadoInput> seleccionados)
    {
        // AGREGAR ESTOS LOGS AL INICIO:
        Console.WriteLine($"[DEBUG] Usuario autenticado: {User.Identity.IsAuthenticated}");
        if (User.Identity.IsAuthenticated)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            Console.WriteLine($"[DEBUG] UsuarioId obtenido: {userId}");
            Console.WriteLine($"[DEBUG] UserName: {User.Identity.Name}");
        }

        if (seleccionados == null)
        {
            return RedirectToAction("SeleccionMultiple", "Productos");
        }

        var seleccionadosValidos = seleccionados
            .Where(p => p.Seleccionado && p.Cantidad > 0)
            .ToList();

        if (!seleccionadosValidos.Any())
        {
            return RedirectToAction("SeleccionMultiple", "Productos");
        }

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

        var pedido = new Pedido
        {
            Fecha = DateTime.Now,
            SucursalId = sucursal.Id,
            PedidoProductos = new List<PedidoProducto>(),
            Estado = "Preparándose",
            UsuarioId = User.Identity.IsAuthenticated ? User.FindFirstValue(ClaimTypes.NameIdentifier) : null
        };

        decimal total = 0;
        foreach (var item in seleccionadosValidos)
        {
            var producto = await _context.Productos.FindAsync(item.ProductoId);
            if (producto != null)
            {
                decimal subtotal = producto.Precio * item.Cantidad;
                total += subtotal;

                pedido.PedidoProductos.Add(new PedidoProducto
                {
                    ProductoId = producto.Id,
                    Cantidad = item.Cantidad,
                    Precio = producto.Precio
                });
            }
        }

        pedido.Total = total;
        _context.Pedidos.Add(pedido);
        await _context.SaveChangesAsync();

        // AGREGAR PUNTOS AL USUARIO SI ESTÁ AUTENTICADO
        if (User.Identity.IsAuthenticated && total > 0)
        {
            await AgregarPuntosAUsuario(pedido.UsuarioId, total);
        }

        CookieOptions options = new CookieOptions
        {
            Expires = DateTimeOffset.Now.AddMinutes(30)
        };
        Response.Cookies.Append("PedidoTemporalId", pedido.Id.ToString(), options);

        return RedirectToAction("Resumen", new { id = pedido.Id });
    }

    [HttpPost]
    public async Task<IActionResult> CrearDesdeCarrito(string pedidoJson)
    {
        if (string.IsNullOrEmpty(pedidoJson))
        {
            return RedirectToAction("Index", "Home");
        }

        try
        {
            // Usa la clase CarritoItem sin el namespace, ya que ya deberías tener
            // using Proyecto1_MZ_MJ.Models; al inicio del archivo
            var itemsCarrito = System.Text.Json.JsonSerializer.Deserialize<List<CarritoItem>>(pedidoJson);

            if (itemsCarrito == null || !itemsCarrito.Any())
            {
                return RedirectToAction("Index", "Home");
            }

            // El resto del método permanece igual
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

            var pedido = new Pedido
            {
                Fecha = DateTime.Now,
                SucursalId = sucursal.Id,
                PedidoProductos = new List<PedidoProducto>(),
                Estado = "Preparándose",
                UsuarioId = User.Identity.IsAuthenticated ? User.FindFirstValue(ClaimTypes.NameIdentifier) : null // ← ESTA LÍNEA ES CRÍTICA
            };

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

                        pedido.PedidoProductos.Add(new PedidoProducto
                        {
                            ProductoId = productoId,
                            Cantidad = item.Cantidad,
                            Precio = item.Precio,
                            Producto = producto
                        });
                    }
                }
            }

            pedido.Total = total;

            _context.Pedidos.Add(pedido);
            await _context.SaveChangesAsync();

            CookieOptions options = new CookieOptions
            {
                Expires = DateTimeOffset.Now.AddMinutes(30)
            };
            Response.Cookies.Append("PedidoTemporalId", pedido.Id.ToString(), options);

            TempData["LimpiarCarrito"] = true;

            return RedirectToAction("Resumen", new { id = pedido.Id });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al procesar el carrito: {ex.Message}");
            return RedirectToAction("Index", "Home");
        }
    }

    [HttpPost]
    public IActionResult ProcesarCarrito(string pedidoJson)
    {
        if (string.IsNullOrEmpty(pedidoJson))
        {
            return RedirectToAction("Index", "Home");
        }

        // Guardamos los datos del carrito en TempData para recuperarlos después
        TempData["DatosCarrito"] = pedidoJson;

        // Redireccionamos a la selección de punto de recolección
        return RedirectToAction("Seleccionar", "Recoleccion");
    }


    // Definición de clase auxiliar ElementoCarrito para deserialización
    public class ElementoCarrito
    {
        public int ProductoId { get; set; }
        public int Cantidad { get; set; }
    }

    // Agregar este método al PedidosController existente

    public async Task<IActionResult> CrearPedido([FromBody] List<PedidoDetalle> detalles)
    {
        // ✅ 1. LOGS AL INICIO para verificar autenticación
        Console.WriteLine($"[DEBUG] CrearPedido iniciado");
        Console.WriteLine($"[DEBUG] Usuario autenticado: {User.Identity.IsAuthenticated}");
        if (User.Identity.IsAuthenticated)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            Console.WriteLine($"[DEBUG] UsuarioId obtenido: {userId}");
            Console.WriteLine($"[DEBUG] UserName: {User.Identity.Name}");
        }

        if (detalles == null || !detalles.Any())
            return BadRequest("Carrito vacío");

        // Obtenemos la sucursal seleccionada de la sesión
        var sucursalId = HttpContext.Session.GetInt32("SucursalSeleccionada");

        // Si no hay sucursal seleccionada, intentamos obtener la primera sucursal
        if (sucursalId == null)
        {
            var primeraSucursal = await _context.Sucursales.FirstOrDefaultAsync();
            if (primeraSucursal != null)
            {
                sucursalId = primeraSucursal.Id;
                HttpContext.Session.SetInt32("SucursalSeleccionada", sucursalId.Value);
            }
            else
            {
                return BadRequest("No se ha seleccionado una sucursal y no hay sucursales disponibles.");
            }
        }

        var pedido = new Pedido
        {
            Fecha = DateTime.Now,
            SucursalId = sucursalId.Value,
            PedidoProductos = new List<PedidoProducto>(),
            UsuarioId = User.Identity.IsAuthenticated ? User.FindFirstValue(ClaimTypes.NameIdentifier) : null
        };

        // ✅ 2. LOG DESPUÉS de crear el pedido para verificar UsuarioId
        Console.WriteLine($"[DEBUG] Pedido creado con UsuarioId: {pedido.UsuarioId}");

        // Calcular el total y agregar productos
        decimal totalCalculado = 0;
        foreach (var detalle in detalles)
        {
            var producto = await _context.Productos.FindAsync(detalle.ProductoId);
            if (producto != null)
            {
                decimal subtotal = detalle.Cantidad * detalle.PrecioUnitario;
                totalCalculado += subtotal;

                pedido.PedidoProductos.Add(new PedidoProducto
                {
                    ProductoId = detalle.ProductoId,
                    Cantidad = detalle.Cantidad,
                    Precio = detalle.PrecioUnitario
                });
            }
        }

        pedido.Total = totalCalculado;
        Console.WriteLine($"[DEBUG] Total calculado: {totalCalculado}");

        _context.Pedidos.Add(pedido);
        await _context.SaveChangesAsync();
        Console.WriteLine("[DEBUG] Pedido guardado en BD");

        // **AGREGAR PUNTOS AL USUARIO SI ESTÁ AUTENTICADO**
        if (User.Identity.IsAuthenticated && totalCalculado > 0)
        {
            Console.WriteLine($"[DEBUG] Llamando AgregarPuntosAUsuario con UsuarioId: {pedido.UsuarioId}, Total: {totalCalculado}");
            await AgregarPuntosAUsuario(pedido.UsuarioId, totalCalculado);
        }
        else
        {
            Console.WriteLine($"[DEBUG] NO se agregaron puntos - Autenticado: {User.Identity.IsAuthenticated}, Total: {totalCalculado}");
        }

        Console.WriteLine($"[DEBUG] CrearPedido terminado exitosamente");
        return Ok(new { mensaje = "Pedido creado exitosamente", pedido.Id });

        // ❌ NUNCA pongas código después del return - NO se ejecutará
    }

    // Método privado para agregar puntos al usuario
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



}