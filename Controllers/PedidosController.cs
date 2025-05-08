using Microsoft.AspNetCore.Mvc;
using ProyectoIdentity.Datos;
using ProyectoIdentity.Models;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;


namespace ProyectoIdentity.Controllers
{
    public class PedidosController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PedidosController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public IActionResult Crear(int productoId, int cantidad)
        {
            // 1) Obtén (o crea) el Pedido activo
            var pedido = new Pedido();
            _context.Pedidos.Add(pedido);
            _context.SaveChanges();

            // 2) Trae el producto para conocer su precio
            var producto = _context.Productos.Find(productoId);

            if (producto == null) return NotFound();

            // 3) Crea la línea de pedido
            var linea = new PedidoProducto
            {
                PedidoId = pedido.Id,
                ProductoId = productoId,
                Cantidad = cantidad,
                Precio = producto.Precio ?? 0m,
                Total = cantidad * (producto.Precio ?? 0m)
            };

            _context.PedidoProductos.Add(linea);
            _context.SaveChanges();

            return RedirectToAction("Resumen", "Pedidos", new { id = pedido.Id });
        }
        public async Task<IActionResult> Detalle(int id)
        {
            var pedido = await _context.Pedidos
                .Include(p => p.PedidoProductos!)
                    .ThenInclude(pp => pp.Producto)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pedido == null)
                return NotFound();

            return View("Resumen", pedido); // Usa la misma vista si quieres
        }

        public async Task<IActionResult> Resumen()
        {
            var ultimoPedido = await _context.Pedidos
                .OrderByDescending(p => p.Id)
                .FirstOrDefaultAsync();

            if (ultimoPedido == null)
            {
                return View("ResumenVacio");
            }

            return RedirectToAction("Detalle", new { id = ultimoPedido.Id });
        }


        public async Task<IActionResult> Admin()
        {
            var pedidos = await _context.Pedidos.ToListAsync();
            return View(pedidos);
        }

        [HttpPost]
        public async Task<IActionResult> CrearDesdeCarrito(string pedidoJson)
        {
            if (string.IsNullOrEmpty(pedidoJson))
            {
                return RedirectToAction("Menu", "Productos");
            }

            try
            {
                // Deserializar el JSON del carrito usando la clase pública ItemCarrito
                var itemsCarrito = JsonSerializer.Deserialize<List<CartItem>>(pedidoJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                // Crear un nuevo pedido
                var pedido = new Pedido();
                _context.Pedidos.Add(pedido);
                await _context.SaveChangesAsync();

                // Agregar productos al pedido
                foreach (var item in itemsCarrito)
                {
                    var producto = await _context.Productos.FindAsync(int.Parse(item.Id));
                    if (producto != null)
                    {
                        var pedidoProducto = new PedidoProducto
                        {
                            PedidoId = pedido.Id,
                            ProductoId = producto.Id,
                            Cantidad = item.Cantidad,
                            Precio = producto.Precio ?? 0,
                            Total = (producto.Precio ?? 0) * item.Cantidad
                        };

                        _context.PedidoProductos.Add(pedidoProducto);
                    }
                }

                await _context.SaveChangesAsync();

                return RedirectToAction("Menu", "Productos");
            }
            catch (Exception ex)
            {
                // Puedes registrar el error si tienes logging
                return RedirectToAction("Error", "Home");
            }
        }


    }
}
