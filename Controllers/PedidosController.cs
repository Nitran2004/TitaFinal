using Microsoft.AspNetCore.Mvc;
using ProyectoIdentity.Datos;
using ProyectoIdentity.Models;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using ProyectoIdentity.Models.DTOs;
using Microsoft.AspNetCore.Authorization;


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
            .Include(p => p.Sucursal) // Incluir la sucursal relacionada
            .FirstOrDefaultAsync(p => p.Id == id);

        if (pedido == null) return NotFound();

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

    [HttpPost]
    public async Task<IActionResult> CrearPedido([FromBody] List<PedidoDetalle> detalles)
    {
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
            Total = detalles.Sum(d => d.Cantidad * d.PrecioUnitario),
            SucursalId = sucursalId.Value,
            Detalles = detalles
        };

        _context.Pedidos.Add(pedido);
        await _context.SaveChangesAsync();

        return Ok(new { mensaje = "Pedido creado exitosamente", pedido.Id });
    }
    [HttpPost]
    public async Task<IActionResult> AgregarSeleccionados(List<ProductoSeleccionadoInput> seleccionados)
    {
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
            PedidoProductos = new List<PedidoProducto>()
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

        // Guardar ID del pedido en una cookie por 30 minutos
        CookieOptions options = new CookieOptions
        {
            Expires = DateTimeOffset.Now.AddMinutes(30)
        };
        Response.Cookies.Append("PedidoTemporalId", pedido.Id.ToString(), options);

        return RedirectToAction("Seleccionar", "Recoleccion");
        //return RedirectToAction("Resumen", new { id = pedido.Id });
    }

    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> ResumenAdmin()
    {
        var pedidos = await _context.Pedidos
            .Include(p => p.PedidoProductos!)
                .ThenInclude(pp => pp.Producto)
            .Include(p => p.Sucursal)
            .OrderByDescending(p => p.Fecha)
            .ToListAsync();

        return View(pedidos);
    }
    public async Task<IActionResult> VerPedidoTemporal()
    {
        int? pedidoId = null;

        // 1. Intentar obtener el ID del pedido de la sesión (prioridad más alta)
        pedidoId = HttpContext.Session.GetInt32("PedidoActualId");

        // 2. Si no está en la sesión, intentar obtenerlo de la cookie PedidoActualId
        if (!pedidoId.HasValue && Request.Cookies.TryGetValue("PedidoActualId", out string pedidoActualIdStr))
        {
            if (int.TryParse(pedidoActualIdStr, out int id))
            {
                pedidoId = id;
            }
        }

        // 3. Si aún no hay ID, intentar obtenerlo de la cookie PedidoTemporalId
        if (!pedidoId.HasValue && Request.Cookies.TryGetValue("PedidoTemporalId", out string pedidoTemporalIdStr))
        {
            if (int.TryParse(pedidoTemporalIdStr, out int id))
            {
                pedidoId = id;
            }
        }

        // Si encontramos un ID de pedido, intentar cargar el pedido
        if (pedidoId.HasValue)
        {
            var pedido = await _context.Pedidos
                .Include(p => p.PedidoProductos)
                    .ThenInclude(pp => pp.Producto)
                .Include(p => p.Sucursal)
                .FirstOrDefaultAsync(p => p.Id == pedidoId.Value);

            if (pedido != null)
            {
                // Actualizar ambas formas de almacenamiento para futuras referencias
                HttpContext.Session.SetInt32("PedidoActualId", pedido.Id);

                Response.Cookies.Append("PedidoActualId", pedido.Id.ToString(), new CookieOptions
                {
                    Expires = DateTimeOffset.Now.AddDays(1)
                });

                return View("Resumen", pedido);
            }
        }

        // Si no se encuentra ningún pedido, buscar el pedido más reciente
        var pedidoMasReciente = await _context.Pedidos
            .Include(p => p.PedidoProductos)
                .ThenInclude(pp => pp.Producto)
            .Include(p => p.Sucursal)
            .OrderByDescending(p => p.Fecha)
            .FirstOrDefaultAsync();

        if (pedidoMasReciente != null)
        {
            // Actualizar ambas formas de almacenamiento para futuras referencias
            HttpContext.Session.SetInt32("PedidoActualId", pedidoMasReciente.Id);

            Response.Cookies.Append("PedidoActualId", pedidoMasReciente.Id.ToString(), new CookieOptions
            {
                Expires = DateTimeOffset.Now.AddDays(1)
            });

            return View("Resumen", pedidoMasReciente);
        }

        // Si no hay pedidos, redirigir al inicio
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

    [HttpPost]
    public async Task<IActionResult> ResumenPedido()
    {
        int? sucursalId = HttpContext.Session.GetInt32("SucursalSeleccionada");
        if (sucursalId == null) return RedirectToAction("SeleccionarSucursal");

        var sucursal = await _context.Sucursales.FindAsync(sucursalId);
        ViewBag.Sucursal = sucursal;

        var pedido = await _context.Pedidos
            .Include(p => p.PedidoProductos)
            .ThenInclude(pp => pp.Producto)
            .Include(p => p.Sucursal)
            .OrderByDescending(p => p.Id)
            .FirstOrDefaultAsync();

        return View(pedido);
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
    [ValidateAntiForgeryToken] // Si estás usando protección CSRF
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
                Total = 0,
                SucursalId = sucursal.Id,
                PedidoProductos = new List<PedidoProducto>()
            };

            // Agregamos los productos al pedido
            foreach (var producto in productos)
            {
                pedido.Total += producto.Precio;
                pedido.PedidoProductos.Add(new PedidoProducto
                {
                    ProductoId = producto.Id,
                    Cantidad = 1,
                    Precio = producto.Precio
                });
            }

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

    [HttpPost]
    public async Task<IActionResult> GuardarComentario(int pedidoId, int calificacion, string comentario)
    {
        var pedido = await _context.Pedidos.FindAsync(pedidoId);

        if (pedido == null)
        {
            return NotFound();
        }

        // Verificar que el pedido esté en estado "Entregado"
        if (pedido.Estado != "Entregado")
        {
            return BadRequest("Solo se pueden comentar pedidos entregados");
        }

        // Asignar comentario y calificación
        pedido.Comentario = comentario;
        pedido.Calificacion = calificacion;
        pedido.ComentarioEnviado = true;

        await _context.SaveChangesAsync();

        // Redireccionar al resumen del pedido
        return RedirectToAction("Resumen", new { id = pedidoId });
    }

    // En PedidosController.cs
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
            PedidoProductos = new List<PedidoProducto>()
        };

        // Buscar punto de recolección
        var puntoRecoleccion = await _context.CollectionPoints.FindAsync(request.CollectionPointId);
        if (puntoRecoleccion == null)
        {
            return NotFound("Punto de recolección no encontrado");
        }

        pedido.SucursalId = puntoRecoleccion.SucursalId;


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

        // Devolver el ID del pedido creado
        return Ok(new { id = pedido.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ProcesarSeleccionMultiple(List<ProductoSeleccionadoInput> seleccionados)
    {
        if (seleccionados == null)
        {
            return RedirectToAction("SeleccionMultiple", "Productos");
        }

        // Filtrar solo los productos que han sido seleccionados y tienen cantidad > 0
        var seleccionadosValidos = seleccionados
            .Where(p => p.Seleccionado && p.Cantidad > 0)
            .ToList();

        if (!seleccionadosValidos.Any())
        {
            // Si no hay productos seleccionados válidos, redirigir de vuelta
            return RedirectToAction("SeleccionMultiple", "Productos");
        }

        // Obtener la sucursal (asumimos que existe al menos una)
        var sucursal = await _context.Sucursales.FirstOrDefaultAsync();
        if (sucursal == null)
        {
            // Crear una sucursal predeterminada si no existe ninguna
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

        // Crear el pedido
        var pedido = new Pedido
        {
            Fecha = DateTime.Now,
            SucursalId = sucursal.Id,
            PedidoProductos = new List<PedidoProducto>(),
            Estado = "Preparándose"
        };

        // Agregar productos al pedido
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

        // Guardar ID del pedido en una cookie
        CookieOptions options = new CookieOptions
        {
            Expires = DateTimeOffset.Now.AddMinutes(30)
        };
        Response.Cookies.Append("PedidoTemporalId", pedido.Id.ToString(), options);

        // Redirigir a la página de resumen del pedido
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
                Estado = "Preparándose"
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



    //[HttpPost]
    //public async Task<IActionResult> AgregarSeleccionados(List<ProductoSeleccionadoInput> seleccionados, int? puntoRecoleccionId = null)
    //{
    //    var seleccionadosValidos = seleccionados
    //        .Where(p => p.Seleccionado && p.Cantidad > 0)
    //        .ToList();

    //    if (!seleccionadosValidos.Any())
    //    {
    //        return BadRequest("Datos inválidos");
    //    }

    //    // Obtener la primera sucursal directamente de la base de datos
    //    var sucursal = await _context.Sucursales.FirstOrDefaultAsync();
    //    if (sucursal == null)
    //    {
    //        // Crear una sucursal si no existe ninguna
    //        sucursal = new Sucursal
    //        {
    //            Nombre = "Verace Pizza",
    //            Direccion = "Av. de los Shyris N35-52",
    //            Latitud = -0.180653,
    //            Longitud = -78.487834
    //        };
    //        _context.Sucursales.Add(sucursal);
    //        await _context.SaveChangesAsync();
    //    }

    //    var pedido = new Pedido
    //    {
    //        Fecha = DateTime.Now,
    //        SucursalId = sucursal.Id,
    //        PedidoProductos = new List<PedidoProducto>()
    //    };

    //    decimal total = 0;

    //    foreach (var item in seleccionadosValidos)
    //    {
    //        var producto = await _context.Productos.FindAsync(item.ProductoId);
    //        if (producto != null)
    //        {
    //            decimal subtotal = producto.Precio * item.Cantidad;
    //            total += subtotal;

    //            pedido.PedidoProductos.Add(new PedidoProducto
    //            {
    //                ProductoId = producto.Id,
    //                Cantidad = item.Cantidad,
    //                Precio = producto.Precio
    //            });
    //        }
    //    }

    //    pedido.Total = total;

    //    _context.Pedidos.Add(pedido);
    //    await _context.SaveChangesAsync();

    //    // Guardar ID del pedido en una cookie por 30 minutos
    //    CookieOptions options = new CookieOptions
    //    {
    //        Expires = DateTimeOffset.Now.AddMinutes(30)
    //    };
    //    Response.Cookies.Append("PedidoTemporalId", pedido.Id.ToString(), options);

    //    // Si se proporciona un punto de recolección, redirigir directamente a ese punto
    //    if (puntoRecoleccionId.HasValue)
    //    {
    //        return RedirectToAction("Confirmar", "Recoleccion", new { id = puntoRecoleccionId.Value, pedidoId = pedido.Id });
    //    }

    //    // Si no hay punto de recolección, seguir con el flujo normal
    //    return RedirectToAction("Seleccionar", "Recoleccion");
    //}

}