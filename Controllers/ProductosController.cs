using Microsoft.AspNetCore.Mvc;
using ProyectoIdentity.Datos;
using Microsoft.EntityFrameworkCore;
using ProyectoIdentity.Models;
using ProyectoIdentity.Models.DTOs;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;

namespace Proyecto1_MZ_MJ.Controllers
{
    public class ProductosController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductosController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Este método ya existe, mantenerlo igual
        public async Task<IActionResult> Index()
        {
            var productos = await _context.Productos.ToListAsync();
            return View(productos);
        }

        // Método para mostrar detalle mejorado de un producto
        public async Task<IActionResult> Detalle(int id)
        {
            var producto = await _context.Productos.FindAsync(id);
            if (producto == null) return NotFound();
            return View(producto);
        }

        // Método para selección múltiple
        public async Task<IActionResult> SeleccionMultiple()
        {
            var productos = await _context.Productos.ToListAsync();
            var categorias = productos.Select(p => p.Categoria).Distinct().ToList();

            ViewBag.Categorias = categorias;

            return View(productos);
        }

        // Método para agregar productos seleccionados al carrito y seguir comprando
        [HttpPost]
        public IActionResult AgregarYSeguir(List<ProductoSeleccionadoInput> seleccionados)
        {
            // Filtrar solo productos seleccionados
            var seleccionadosValidos = seleccionados
                .Where(p => p.Seleccionado && p.Cantidad > 0)
                .ToList();

            if (!seleccionadosValidos.Any())
            {
                return RedirectToAction("SeleccionMultiple");
            }

            // Guardar datos en TempData para mostrar mensaje de éxito
            TempData["ProductosAgregados"] = seleccionadosValidos.Count;

            return RedirectToAction("SeleccionMultiple");
        }

        // Método para mostrar todos los productos por categoría pero funcional
        // (actualizado para funcionar como página independiente)
        public async Task<IActionResult> PorCategoria(string categoria)
        {
            var productos = await _context.Productos.ToListAsync();

            if (!string.IsNullOrEmpty(categoria) && categoria.ToLower() != "todas")
            {
                productos = productos.Where(p => p.Categoria == categoria).ToList();
            }

            ViewBag.CategoriaActual = categoria;
            ViewBag.Categorias = await _context.Productos
                .Select(p => p.Categoria)
                .Distinct()
                .ToListAsync();

            return View(productos);
        }

        // Métodos existentes para mostrar categorías específicas
        public async Task<IActionResult> Menu()
        {
            var productos = await _context.Productos.ToListAsync();
            return View(productos);
        }

        public async Task<IActionResult> Pizzas()
        {
            var productos = await _context.Productos.ToListAsync();
            return View(productos);
        }

        public async Task<IActionResult> Sanduches()
        {
            var productos = await _context.Productos.ToListAsync();
            return View(productos);
        }

        public async Task<IActionResult> Picadas()
        {
            var productos = await _context.Productos.ToListAsync();
            return View(productos);
        }

        public async Task<IActionResult> Bebidas()
        {
            var productos = await _context.Productos.ToListAsync();
            return View(productos);
        }

        public async Task<IActionResult> Promos()
        {
            var productos = await _context.Productos.ToListAsync();
            return View(productos);
        }

        public async Task<IActionResult> Cerveza()
        {
            var productos = await _context.Productos.ToListAsync();
            return View(productos);
        }

        public async Task<IActionResult> Cocteles()
        {
            var productos = await _context.Productos.ToListAsync();
            return View(productos);
        }

        public async Task<IActionResult> Shots()
        {
            var productos = await _context.Productos.ToListAsync();
            return View(productos);
        }

        // 1. Primero, modificación al método en ProductosController para la selección múltiple
        // Ubicación: ProductosController.cs, método de procesamiento del formulario

        // En ProductosController.cs, actualiza este método:

        [HttpPost]
        public IActionResult ProcesarSeleccionMultiple(List<ProductoSeleccionadoInput> seleccionados)
        {
            // Filtramos solo los productos seleccionados y con cantidad > 0
            var seleccionadosValidos = seleccionados
                .Where(p => p.Seleccionado && p.Cantidad > 0)
                .ToList();

            if (!seleccionadosValidos.Any())
            {
                TempData["Error"] = "No se seleccionaron productos";
                return RedirectToAction("SeleccionMultiple");
            }

            // Guardamos en TempData
            TempData["ProductosSeleccionados"] = System.Text.Json.JsonSerializer.Serialize(seleccionadosValidos);

            // Importante: mantener TempData hasta que se use
            TempData.Keep("ProductosSeleccionados");

            // Redireccionamos a la selección de punto de recolección
            return RedirectToAction("Seleccionar", "Recoleccion");
        }

        // 2. Modificación al PedidosController para el carrito
        // Ubicación: PedidosController.cs

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

        // 3. Modificación al RecoleccionController
        // Ubicación: RecoleccionController.cs

        public async Task<IActionResult> Seleccionar()
        {
            // Obtener todos los puntos de recolección
            var puntosRecoleccion = await _context.CollectionPoints
                .Include(p => p.Sucursal)
                .ToListAsync();

            // Verificar que existan puntos de recolección
            if (!puntosRecoleccion.Any())
            {
                return RedirectToAction("Index", "Home", new { mensaje = "No hay puntos de recolección disponibles" });
            }

            return View(puntosRecoleccion);
        }

        [HttpPost]
        public async Task<IActionResult> Confirmar(int id)
        {
            // Obtener el punto de recolección
            var puntoRecoleccion = await _context.CollectionPoints
                .Include(p => p.Sucursal)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (puntoRecoleccion == null)
            {
                return NotFound();
            }

            ViewBag.PuntoRecoleccionId = id;
            return View(puntoRecoleccion);
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

            return RedirectToAction("Resumen", "Pedidos", new { id = pedidoId });
        }

        // Métodos privados para crear los pedidos
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