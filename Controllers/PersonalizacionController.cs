using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoIdentity.Datos;
using ProyectoIdentity.Models;
using System.Text.Json;

namespace ProyectoIdentity.Controllers
{
    public class PersonalizacionController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PersonalizacionController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Vista principal - mostrar todos los productos
        public async Task<IActionResult> Index()
        {
            var productos = await _context.Productos
                .Where(p => p.Categoria == "Pizza" || p.Categoria == "Sánduches" || p.Categoria == "Picadas")
                .ToListAsync();
            return View(productos);
        }

        // Detalle del producto con personalización
        public async Task<IActionResult> Detalle(int id)
        {
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

                // Calcular ahorro interno
                decimal ahorroInterno = CalcularAhorro(producto, request.IngredientesRemovidos);

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

                return Json(new
                {
                    success = true,
                    message = $"{producto.Nombre} agregado al carrito. Ahorro interno: ${ahorroInterno:F2}",
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

        [HttpPost]
        public async Task<IActionResult> ProcesarPedido([FromBody] PedidoPersonalizadoRequest request)
        {
            try
            {
                var carrito = GetCarrito();
                if (!carrito.Any())
                    return Json(new { success = false, message = "El carrito está vacío" });

                // Obtener sucursal
                var sucursal = await _context.Sucursales.FirstOrDefaultAsync();

                // Crear pedido
                var pedido = new Pedido
                {
                    Fecha = DateTime.Now,
                    UsuarioId = null,
                    Estado = "Preparándose",
                    Total = carrito.Sum(c => c.Subtotal),
                    TipoServicio = "Servir aquí",
                    SucursalId = sucursal.Id
                };

                _context.Pedidos.Add(pedido);
                await _context.SaveChangesAsync();

                // Crear detalles CON personalización
                foreach (var item in carrito)
                {
                    var detalle = new PedidoDetalle
                    {
                        PedidoId = pedido.Id,
                        ProductoId = item.Id,
                        Cantidad = item.Cantidad,
                        PrecioUnitario = item.Precio,
                        IngredientesRemovidos = JsonSerializer.Serialize(item.IngredientesRemovidos),
                        NotasEspeciales = item.NotasEspeciales
                    };

                    _context.PedidoDetalles.Add(detalle);
                    await _context.SaveChangesAsync();
                }

                HttpContext.Session.Remove("CarritoPersonalizado");
                return Json(new { success = true, pedidoId = pedido.Id });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }
        public async Task<IActionResult> Confirmacion(int id)
        {
            var pedido = await _context.Pedidos
                .Include(p => p.Detalles)
                .ThenInclude(d => d.Producto)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pedido == null) return NotFound();
            return View(pedido);
        }

        // Panel de administrador
        public async Task<IActionResult> AdminAnalisis()
        {
            var analisis = await GenerarAnalisisSimple();
            return View(analisis);
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
        public string? Observaciones { get; set; }
    }
}