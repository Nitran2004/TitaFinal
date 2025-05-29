using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoIdentity.Datos;
using ProyectoIdentity.Models;
using System.Security.Claims;
using Newtonsoft.Json;

namespace ProyectoIdentity.Controllers
{
    public class CuponesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CuponesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Vista principal de cupones disponibles para usuarios
        public async Task<IActionResult> Index()
        {
            var cuponesActivos = await _context.Cupones
                .Where(c => c.Activo && c.FechaFin >= DateTime.Now.Date)
                .ToListAsync();

            var model = new CuponesDisponiblesViewModel();

            foreach (var cupon in cuponesActivos)
            {
                var cuponVM = new CuponViewModel
                {
                    Id = cupon.Id,
                    Nombre = cupon.Nombre,
                    Descripcion = cupon.Descripcion,
                    TipoPromocion = cupon.TipoPromocion,
                    CodigoQR = cupon.CodigoQR,
                    ImagenCupon = null, // o algún valor byte[] válido                    EsValidoHoy = cupon.EsValidoHoy,
                    DiasAplicables = cupon.DiasAplicables,
                    TextoDescuento = ObtenerTextoDescuento(cupon),
                    ProductosNombres = await ObtenerNombresProductos(cupon.ProductosAplicables),
                    FechaFin = cupon.FechaFin
                };

                // Categorizar cupones
                if (cupon.CategoriasAplicables?.Contains("Promos") == true)
                    model.CuponesPromos.Add(cuponVM);
                else if (cupon.CategoriasAplicables?.Contains("Cervezas") == true)
                    model.CuponesCervezas.Add(cuponVM);
                else if (cupon.CategoriasAplicables?.Contains("Cocteles") == true)
                    model.CuponesCocteles.Add(cuponVM);
                else
                    model.CuponesEspeciales.Add(cuponVM);
            }

            return View(model);
        }

        // Vista del QR individual
        public async Task<IActionResult> DetalleQR(int id)
        {
            var cupon = await _context.Cupones.FindAsync(id);
            if (cupon == null || !cupon.Activo)
                return NotFound();

            var model = new CuponViewModel
            {
                Id = cupon.Id,
                Nombre = cupon.Nombre,
                Descripcion = cupon.Descripcion,
                TipoPromocion = cupon.TipoPromocion,
                CodigoQR = cupon.CodigoQR,
                // Restaurar a lo original:
                ImagenCupon = null, // o algún valor byte[] válido
                EsValidoHoy = cupon.EsValidoHoy,
                DiasAplicables = cupon.DiasAplicables,
                TextoDescuento = ObtenerTextoDescuento(cupon),
                ProductosNombres = await ObtenerNombresProductos(cupon.ProductosAplicables),
                FechaFin = cupon.FechaFin
            };

            return View(model);
        }

        // Vista para escanear QR (solo administradores y cajeros)
        [Authorize(Roles = "Administrador,Cajero")]
        public IActionResult EscanearQR()
        {
            return View(new EscanearQRViewModel());
        }

        // Procesar código QR escaneado
        [HttpPost]
        [Authorize(Roles = "Administrador,Cajero")]
        public async Task<IActionResult> ProcesarQR([FromBody] EscanearQRViewModel model)
        {
            try
            {
                var cupon = await _context.Cupones
                    .FirstOrDefaultAsync(c => c.CodigoQR == model.CodigoQR && c.Activo);

                if (cupon == null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Cupón no válido o no encontrado"
                    });
                }

                if (!cupon.EsValidoHoy)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Este cupón no es válido para el día de hoy"
                    });
                }

                // Verificar si ya fue usado (opcional - depende de si permites usos múltiples)
                var yaUsado = await _context.CuponesCanjeados
                    .AnyAsync(cc => cc.CodigoQR == model.CodigoQR &&
                                   cc.FechaCanje.Date == DateTime.Now.Date);

                if (yaUsado && cupon.TipoPromocion != "MultiUso")
                {
                    return Json(new
                    {
                        success = false,
                        message = "Este cupón ya fue utilizado hoy"
                    });
                }

                // Obtener productos aplicables
                var productosAplicables = await ObtenerProductosAplicables(cupon);

                var response = new
                {
                    success = true,
                    cupon = new
                    {
                        id = cupon.Id,
                        nombre = cupon.Nombre,
                        descripcion = cupon.Descripcion,
                        tipoPromocion = cupon.TipoPromocion,
                        textDescuento = ObtenerTextoDescuento(cupon)
                    },
                    productosAplicables = productosAplicables.Select(p => new
                    {
                        id = p.Id,
                        nombre = p.Nombre,
                        precio = p.Precio,
                        categoria = p.Categoria
                    })
                };

                return Json(response);
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Error al procesar el cupón: " + ex.Message
                });
            }
        }

        // Aplicar cupón y crear pedido
        [HttpPost]
        [Authorize(Roles = "Administrador,Cajero")]
        public async Task<IActionResult> AplicarCupon([FromBody] AplicarCuponRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var cupon = await _context.Cupones
                    .FirstOrDefaultAsync(c => c.CodigoQR == request.CodigoQR);

                if (cupon == null)
                    return Json(new { success = false, message = "Cupón no válido" });

                // Calcular descuento
                var resultado = CalcularDescuento(cupon, request.ProductosSeleccionados);

                // Crear pedido
                var pedido = new Pedido
                {
                    UsuarioId = request.ClienteId ?? User.FindFirstValue(ClaimTypes.NameIdentifier),
                    SucursalId = request.SucursalId,
                    Fecha = DateTime.Now,
                    Estado = "Procesado",
                    Total = resultado.TotalFinal,
                    //Categoria = "Cupón",
                    //Observaciones = $"Cupón aplicado: {cupon.Nombre}"
                };

                _context.Pedidos.Add(pedido);
                await _context.SaveChangesAsync();

                // Crear productos del pedido
                foreach (var producto in request.ProductosSeleccionados)
                {
                    var pedidoProducto = new PedidoProducto
                    {
                        PedidoId = pedido.Id,
                        ProductoId = producto.ProductoId,
                        Cantidad = producto.Cantidad,
                        Precio= producto.Precio,
                        Total = producto.Subtotal
                    };
                    _context.PedidoProductos.Add(pedidoProducto);
                }

                // Registrar uso del cupón
                var cuponCanjeado = new CuponCanjeado
                {
                    CuponId = cupon.Id,
                    UsuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier),
                    ClienteId = request.ClienteId,
                    FechaCanje = DateTime.Now,
                    CodigoQR = request.CodigoQR,
                    DescuentoAplicado = resultado.DescuentoAplicado,
                    ProductosIncluidos = JsonConvert.SerializeObject(request.ProductosSeleccionados),
                    TotalOriginal = resultado.TotalOriginal,
                    TotalConDescuento = resultado.TotalFinal,
                    EstadoCanje = "Procesado"
                };

                _context.CuponesCanjeados.Add(cuponCanjeado);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Json(new
                {
                    success = true,
                    pedidoId = pedido.Id,
                    message = "Cupón aplicado exitosamente"
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Json(new
                {
                    success = false,
                    message = "Error al aplicar cupón: " + ex.Message
                });
            }
        }

        // Métodos auxiliares
        private string ObtenerTextoDescuento(Cupon cupon)
        {
            return cupon.TipoPromocion switch
            {
                "3x2" => "3x2",
                "50%OFF" => "50% OFF",
                "SegundaMitadPrecio" => "Segunda a mitad de precio",
                _ => cupon.DescuentoPorcentaje.HasValue ?
                     $"{cupon.DescuentoPorcentaje}% OFF" :
                     "Descuento especial"
            };
        }

        private async Task<string> ObtenerNombresProductos(string productosIds)
        {
            if (string.IsNullOrEmpty(productosIds))
                return "";

            var ids = productosIds.Split(',')
                                 .Where(x => int.TryParse(x.Trim(), out _))
                                 .Select(x => int.Parse(x.Trim()))
                                 .ToList();

            var productos = await _context.Productos
                .Where(p => ids.Contains(p.Id))
                .Select(p => p.Nombre)
                .ToListAsync();

            return string.Join(", ", productos);
        }

        private async Task<List<Producto>> ObtenerProductosAplicables(Cupon cupon)
        {
            var query = _context.Productos.AsQueryable();

            // Filtrar por IDs específicos
            if (!string.IsNullOrEmpty(cupon.ProductosAplicables))
            {
                var ids = cupon.ProductosAplicables.Split(',')
                                                  .Where(x => int.TryParse(x.Trim(), out _))
                                                  .Select(x => int.Parse(x.Trim()))
                                                  .ToList();
                query = query.Where(p => ids.Contains(p.Id));
            }
            // Filtrar por categorías
            else if (!string.IsNullOrEmpty(cupon.CategoriasAplicables))
            {
                var categorias = cupon.CategoriasAplicables.Split(',')
                                                          .Select(c => c.Trim())
                                                          .ToList();
                query = query.Where(p => categorias.Contains(p.Categoria));
            }

            return await query.ToListAsync();
        }

        private CalculoDescuentoResult CalcularDescuento(Cupon cupon, List<ProductoSeleccionado> productos)
        {
            var totalOriginal = productos.Sum(p => p.Precio * p.Cantidad);
            var descuentoAplicado = 0m;

            switch (cupon.TipoPromocion)
            {
                case "3x2":
                    // Por cada 3 productos, el tercero es gratis
                    foreach (var producto in productos)
                    {
                        var productosGratis = producto.Cantidad / 3;
                        descuentoAplicado += productosGratis * producto.Precio;
                    }
                    break;

                case "50%OFF":
                    descuentoAplicado = totalOriginal * 0.5m;
                    break;

                case "SegundaMitadPrecio":
                    // Segunda unidad a mitad de precio
                    foreach (var producto in productos)
                    {
                        if (producto.Cantidad >= 2)
                        {
                            var descuentosPorProducto = producto.Cantidad / 2;
                            descuentoAplicado += descuentosPorProducto * (producto.Precio * 0.5m);
                        }
                    }
                    break;

                default:
                    if (cupon.DescuentoPorcentaje.HasValue)
                        descuentoAplicado = totalOriginal * (cupon.DescuentoPorcentaje.Value / 100);
                    else if (cupon.DescuentoFijo.HasValue)
                        descuentoAplicado = cupon.DescuentoFijo.Value;
                    break;
            }

            return new CalculoDescuentoResult
            {
                TotalOriginal = totalOriginal,
                DescuentoAplicado = descuentoAplicado,
                TotalFinal = totalOriginal - descuentoAplicado
            };
        }
    }

    // Clases auxiliares
    public class AplicarCuponRequest
    {
        public string CodigoQR { get; set; }
        public string ClienteId { get; set; }
        public int SucursalId { get; set; }
        public List<ProductoSeleccionado> ProductosSeleccionados { get; set; }
    }

    public class CalculoDescuentoResult
    {
        public decimal TotalOriginal { get; set; }
        public decimal DescuentoAplicado { get; set; }
        public decimal TotalFinal { get; set; }
    }
}