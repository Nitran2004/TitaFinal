using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoIdentity.Datos;
using ProyectoIdentity.Models;
using System.Security.Claims;

namespace ProyectoIdentity.Controllers
{
    [Authorize]
    public class FidelizacionController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private const int PUNTOS_POR_DOLAR = 30; // 30 puntos por cada dólar gastado

        public FidelizacionController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Vista principal de fidelización
        public IActionResult Index()
        {
            return View();
        }

        // Mostrar los puntos del usuario actual
        public async Task<IActionResult> MisPuntos()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return RedirectToAction("Acceso", "Cuentas");

            var usuario = await _context.AppUsuario.FindAsync(userId);
            if (usuario == null) return NotFound();

            var model = new MisPuntosViewModel
            {
                PuntosActuales = usuario.PuntosFidelidad ?? 0,
                Usuario = usuario
            };

            return View(model);
        }

        // Vista de recompensas disponibles (usando tu ProductosRecompensa existente)
        public async Task<IActionResult> Recompensas()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var puntosUsuario = 0;

            if (userId != null)
            {
                var usuario = await _context.AppUsuario.FindAsync(userId);
                puntosUsuario = usuario?.PuntosFidelidad ?? 0;
            }

            // Obtener recompensas con las imágenes de la tabla Productos
            var recompensas = await _context.ProductosRecompensa
                .Include(r => r.Producto) // Esto asume que tienes la relación configurada
                .OrderBy(r => r.PuntosNecesarios)
                .ToListAsync();

            // Si no tienes la relación configurada, usa esto en su lugar:

            //var recompensas = await (from r in _context.ProductosRecompensa
            //                         join p in _context.Productos on r.ProductoId equals p.Id into productos
            //                         from producto in productos.DefaultIfEmpty()
            //                         select new ProductoRecompensa
            //                         {
            //                             Id = r.Id,
            //                             ProductoId = r.ProductoId,
            //                             Nombre = r.Nombre,
            //                             PrecioOriginal = r.PrecioOriginal,
            //                             PuntosNecesarios = r.PuntosNecesarios,
            //                             Categoria = r.Categoria,
            //                             Imagen = producto != null ? producto.Imagen : null
            //                         })
            //                       .OrderBy(r => r.PuntosNecesarios)
            //                       .ToListAsync();


            var model = new RecompensasViewModel
            {
                PuntosUsuario = puntosUsuario,
                ProductosRecompensa = recompensas
            };

            return View(model);
        }

        // API para obtener puntos del usuario (para AJAX)
        [HttpGet]
        public async Task<IActionResult> ObtenerPuntos()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return Json(new { puntos = 0 });

            var usuario = await _context.AppUsuario.FindAsync(userId);
            var puntos = usuario?.PuntosFidelidad ?? 0;

            return Json(new { puntos = puntos });
        }

        // Método para agregar puntos cuando se completa un pedido
        public async Task<bool> AgregarPuntosPorPedido(string usuarioId, decimal totalPedido)
        {
            if (string.IsNullOrEmpty(usuarioId)) return false;

            var usuario = await _context.AppUsuario.FindAsync(usuarioId);
            if (usuario == null) return false;

            // Calcular puntos ganados (30 puntos por dólar)
            int puntosGanados = (int)(totalPedido * PUNTOS_POR_DOLAR);

            // Agregar puntos al usuario
            usuario.PuntosFidelidad = (usuario.PuntosFidelidad ?? 0) + puntosGanados;

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
            await _context.SaveChangesAsync();

            return true;
        }

        // Método actualizado para CanjearRecompensa - redirige al resumen

        [HttpPost]
        public async Task<IActionResult> CanjearRecompensa(int recompensaId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return RedirectToAction("Acceso", "Cuentas");

            var usuario = await _context.AppUsuario.FindAsync(userId);
            var productoRecompensa = await _context.ProductosRecompensa
                .Include(r => r.Producto) // Incluir producto para la imagen
                .FirstOrDefaultAsync(r => r.Id == recompensaId);

            if (usuario == null || productoRecompensa == null)
                return NotFound();

            var puntosActuales = usuario.PuntosFidelidad ?? 0;

            if (puntosActuales < productoRecompensa.PuntosNecesarios)
            {
                TempData["Error"] = "No tienes suficientes puntos para canjear esta recompensa.";
                return RedirectToAction("Recompensas");
            }

            // Descontar puntos
            usuario.PuntosFidelidad = puntosActuales - productoRecompensa.PuntosNecesarios;

            // Crear registro en tu tabla HistorialCanje existente
            var historialCanje = new HistorialCanje
            {
                UsuarioId = userId,
                ProductoRecompensaId = recompensaId,
                PuntosUtilizados = productoRecompensa.PuntosNecesarios,
                FechaCanje = DateTime.Now
            };

            // Crear registro de transacción
            var transaccion = new TransaccionPuntos
            {
                UsuarioId = userId,
                Puntos = -productoRecompensa.PuntosNecesarios,
                Tipo = "Canje",
                Descripcion = $"Canje de recompensa: {productoRecompensa.Nombre}",
                Fecha = DateTime.Now
            };

            _context.HistorialCanjes.Add(historialCanje);
            _context.TransaccionesPuntos.Add(transaccion);
            await _context.SaveChangesAsync();

            // ✅ NUEVO: Redirigir al resumen de canje en lugar de mostrar mensaje
            return RedirectToAction("ResumenCanje", new { id = historialCanje.Id });
        }

        // ✅ NUEVO: Método para mostrar el resumen del canje
        public async Task<IActionResult> ResumenCanje(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return RedirectToAction("Acceso", "Cuentas");

            // Obtener el canje específico
            var canje = await _context.HistorialCanjes
                .Include(h => h.ProductoRecompensa)
                    .ThenInclude(pr => pr.Producto) // Incluir producto para la imagen
                .FirstOrDefaultAsync(h => h.Id == id && h.UsuarioId == userId);

            if (canje == null) return NotFound();

            // Obtener usuario actualizado para puntos restantes
            var usuario = await _context.AppUsuario.FindAsync(userId);

            // Crear modelo para la vista
            var model = new ResumenCanjeViewModel
            {
                Canje = canje,
                PuntosRestantes = usuario?.PuntosFidelidad ?? 0,
                CodigoCanje = GenerarCodigoCanje(canje.Id),
                Usuario = usuario
            };

            return View(model);
        }
        // ✅ NUEVO: Método auxiliar para generar código de canje

        // Método para canjear múltiples recompensas
        [HttpPost]
        public async Task<IActionResult> CanjearMultiplesRecompensas(List<RecompensaSeleccionadaInput> recompensasSeleccionadas)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return RedirectToAction("Acceso", "Cuentas");

            // Filtrar solo las recompensas seleccionadas
            var seleccionadas = recompensasSeleccionadas
                .Where(r => r.Seleccionada && r.Cantidad > 0)
                .ToList();

            if (!seleccionadas.Any())
            {
                TempData["Error"] = "No seleccionaste ninguna recompensa para canjear.";
                return RedirectToAction("Recompensas");
            }

            var usuario = await _context.AppUsuario.FindAsync(userId);
            if (usuario == null) return NotFound();

            // Obtener todas las recompensas seleccionadas de la base de datos
            var idsRecompensas = seleccionadas.Select(s => s.RecompensaId).ToList();
            var recompensas = await _context.ProductosRecompensa
                .Include(r => r.Producto)
                .Where(r => idsRecompensas.Contains(r.Id))
                .ToListAsync();

            // Calcular el total de puntos necesarios
            int totalPuntosNecesarios = 0;
            var canjesACrear = new List<HistorialCanje>();
            var transaccionesACrear = new List<TransaccionPuntos>();

            foreach (var seleccionada in seleccionadas)
            {
                var recompensa = recompensas.FirstOrDefault(r => r.Id == seleccionada.RecompensaId);
                if (recompensa == null) continue;

                int puntosParaEstaRecompensa = recompensa.PuntosNecesarios * seleccionada.Cantidad;
                totalPuntosNecesarios += puntosParaEstaRecompensa;

                // Crear múltiples registros de canje si la cantidad > 1
                for (int i = 0; i < seleccionada.Cantidad; i++)
                {
                    canjesACrear.Add(new HistorialCanje
                    {
                        UsuarioId = userId,
                        ProductoRecompensaId = seleccionada.RecompensaId,
                        PuntosUtilizados = recompensa.PuntosNecesarios,
                        FechaCanje = DateTime.Now
                    });

                    transaccionesACrear.Add(new TransaccionPuntos
                    {
                        UsuarioId = userId,
                        Puntos = -recompensa.PuntosNecesarios,
                        Tipo = "Canje",
                        Descripcion = $"Canje de recompensa: {recompensa.Nombre}",
                        Fecha = DateTime.Now
                    });
                }
            }

            // Validar que el usuario tenga suficientes puntos
            var puntosActuales = usuario.PuntosFidelidad ?? 0;
            if (puntosActuales < totalPuntosNecesarios)
            {
                TempData["Error"] = $"No tienes suficientes puntos. Necesitas {totalPuntosNecesarios} puntos pero solo tienes {puntosActuales}.";
                return RedirectToAction("Recompensas");
            }

            // Realizar la transacción
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Descontar puntos del usuario
                usuario.PuntosFidelidad = puntosActuales - totalPuntosNecesarios;

                // Agregar todos los canjes y transacciones
                _context.HistorialCanjes.AddRange(canjesACrear);
                _context.TransaccionesPuntos.AddRange(transaccionesACrear);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Crear resumen del canje múltiple
                var resumenCanje = new ResumenCanjeMultipleViewModel
                {
                    Usuario = usuario,
                    CanjesRealizados = canjesACrear,
                    TotalPuntosUtilizados = totalPuntosNecesarios,
                    PuntosRestantes = usuario.PuntosFidelidad ?? 0,
                    FechaCanje = DateTime.Now,
                    CodigoCanje = GenerarCodigoCanjeMultiple(),
                    RecompensasCanjeadas = seleccionadas.Select(s => new RecompensaCanjeadaInfo
                    {
                        Recompensa = recompensas.First(r => r.Id == s.RecompensaId),
                        Cantidad = s.Cantidad,
                        PuntosTotales = recompensas.First(r => r.Id == s.RecompensaId).PuntosNecesarios * s.Cantidad
                    }).ToList()
                };

                // Guardar en TempData para el resumen
                TempData["ResumenCanjeMultiple"] = Newtonsoft.Json.JsonConvert.SerializeObject(new
                {
                    TotalPuntos = totalPuntosNecesarios,
                    CantidadRecompensas = canjesACrear.Count,
                    RecompensasInfo = resumenCanje.RecompensasCanjeadas.Select(r => new {
                        Nombre = r.Recompensa.Nombre,
                        Cantidad = r.Cantidad,
                        Puntos = r.PuntosTotales
                    })
                });

                TempData["Exito"] = $"¡Canje exitoso! Has canjeado {canjesACrear.Count} recompensa(s) por {totalPuntosNecesarios} puntos.";
                return RedirectToAction("ResumenCanjeMultiple");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                TempData["Error"] = "Ocurrió un error al procesar el canje. Por favor, inténtalo de nuevo.";
                return RedirectToAction("Recompensas");
            }
        }

        // Método para mostrar el resumen del canje múltiple
        public async Task<IActionResult> ResumenCanjeMultiple()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return RedirectToAction("Acceso", "Cuentas");

            var resumenJson = TempData["ResumenCanjeMultiple"] as string;
            if (string.IsNullOrEmpty(resumenJson))
            {
                return RedirectToAction("Recompensas");
            }

            var usuario = await _context.AppUsuario.FindAsync(userId);
            var resumenData = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(resumenJson);

            // ✅ Obtener los canjes más recientes del usuario (los que acabamos de crear)
            var canjesRecientes = await _context.HistorialCanjes
                .Include(h => h.ProductoRecompensa)
                    .ThenInclude(pr => pr.Producto)
                .Where(h => h.UsuarioId == userId)
                .OrderByDescending(h => h.FechaCanje)
                .Take((int)resumenData.CantidadRecompensas) // Tomar solo los canjes recientes
                .ToListAsync();

            // ✅ Calcular el valor total ahorrado
            var valorTotalAhorrado = canjesRecientes.Sum(c => c.ProductoRecompensa?.PrecioOriginal ?? 0);

            var model = new ResumenCanjesMultiplesViewModel
            {
                Usuario = usuario,
                TotalPuntosUtilizados = resumenData.TotalPuntos,
                CantidadRecompensas = resumenData.CantidadRecompensas,
                PuntosRestantes = usuario?.PuntosFidelidad ?? 0,
                FechaCanje = DateTime.Now,
                CodigoCanje = GenerarCodigoCanjeMultiple(),
                CanjesRealizados = canjesRecientes, // ✅ Agregar los canjes
                ValorTotalAhorrado = valorTotalAhorrado // ✅ Agregar valor ahorrado
            };

            return View(model);
        }


        // Método para mostrar todos los canjes del usuario
        public async Task<IActionResult> MisCanjes()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return RedirectToAction("Acceso", "Cuentas");

            var usuario = await _context.AppUsuario.FindAsync(userId);
            if (usuario == null) return NotFound();

            // Obtener todos los canjes del usuario agrupados por código de canje
            var canjes = await _context.HistorialCanjes
                .Include(h => h.ProductoRecompensa)
                    .ThenInclude(pr => pr.Producto)
                .Where(h => h.UsuarioId == userId)
                .OrderByDescending(h => h.FechaCanje)
                .ToListAsync();

            // ✅ DEBUG: Verificar qué datos tenemos
            Console.WriteLine($"DEBUG: Usuario ID: {userId}");
            Console.WriteLine($"DEBUG: Total canjes encontrados: {canjes.Count}");

            foreach (var canje in canjes)
            {
                Console.WriteLine($"DEBUG: Canje ID: {canje.Id}, Puntos: {canje.PuntosUtilizados}, Producto: {canje.ProductoRecompensa?.Nombre}");
                if (canje.ProductoRecompensa?.Producto != null)
                {
                    Console.WriteLine($"DEBUG: Producto tiene imagen: {canje.ProductoRecompensa.Producto.Imagen != null}");
                }
            }

            // Agrupar canjes por fecha y hora (canjes múltiples del mismo momento)
            var canjesAgrupados = canjes
                .GroupBy(c => new {
                    Fecha = c.FechaCanje.Date,
                    Hora = c.FechaCanje.Hour,
                    Minuto = c.FechaCanje.Minute
                })
                .Select(group => new CanjeAgrupadoViewModel
                {
                    FechaCanje = group.First().FechaCanje,
                    CanjesIndividuales = group.ToList(),
                    TotalPuntosUtilizados = group.Sum(c => c.PuntosUtilizados),
                    CantidadRecompensas = group.Count(),
                    ValorTotalAhorrado = group.Sum(c => c.ProductoRecompensa?.PrecioOriginal ?? 0),
                    CodigoCanje = GenerarCodigoCanjePorFecha(group.First().FechaCanje, group.First().Id)
                })
                .OrderByDescending(c => c.FechaCanje)
                .ToList();

            // ✅ DEBUG: Verificar datos agrupados
            Console.WriteLine($"DEBUG: Grupos de canjes: {canjesAgrupados.Count}");
            foreach (var grupo in canjesAgrupados)
            {
                Console.WriteLine($"DEBUG: Grupo - Código: {grupo.CodigoCanje}, Productos: {grupo.CantidadRecompensas}, Puntos: {grupo.TotalPuntosUtilizados}");
            }
            var model = new MisCanjesViewModel
            {
                Usuario = usuario,
                CanjesAgrupados = canjesAgrupados,
                PuntosActuales = usuario.PuntosFidelidad ?? 0
            };

            return View(model);
        }

        // Método auxiliar para generar código de canje por fecha
        private string GenerarCodigoCanjePorFecha(DateTime fecha, int canjeId)
        {
            var fechaStr = fecha.ToString("yyyyMMdd");
            var tiempo = fecha.ToString("HHmm");
            return $"CJM-{fechaStr}-{tiempo}";
        }

        // Método auxiliar para generar código de canje múltiple
        private string GenerarCodigoCanjeMultiple()
        {
            var fecha = DateTime.Now.ToString("yyyyMMdd");
            var random = new Random().Next(1000, 9999);
            return $"CJM-{fecha}-{random}";
        }

        private string GenerarCodigoCanje(int canjeId)
        {
            // Formato: CJ-YYYYMMDD-ID (ej: CJ-20250528-00123)
            var fecha = DateTime.Now.ToString("yyyyMMdd");
            var codigo = $"CJ-{fecha}-{canjeId:D5}";
            return codigo;
        }

        // Historial de puntos del usuario
        public async Task<IActionResult> Historial()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return RedirectToAction("Acceso", "Cuentas");

            // Obtener usuario actual
            var usuario = await _context.AppUsuario.FindAsync(userId);
            if (usuario == null) return NotFound();

            // 1. Obtener transacciones de puntos (ganancias y canjes)
            var transacciones = await _context.TransaccionesPuntos
                .Where(t => t.UsuarioId == userId)
                .OrderByDescending(t => t.Fecha)
                .Take(50) // Últimas 50 transacciones
                .ToListAsync();

            // 2. Obtener pedidos del usuario
            var pedidos = await _context.Pedidos
                .Include(p => p.PedidoProductos)
                    .ThenInclude(pp => pp.Producto)
                .Include(p => p.Sucursal)
                .Where(p => p.UsuarioId == userId)
                .OrderByDescending(p => p.Fecha)
                .Take(20) // Últimos 20 pedidos
                .ToListAsync();

            // 3. Obtener recompensas canjeadas
            var canjes = await _context.HistorialCanjes
                .Include(h => h.ProductoRecompensa)
                .Where(h => h.UsuarioId == userId)
                .OrderByDescending(h => h.FechaCanje)
                .Take(20) // Últimos 20 canjes
                .ToListAsync();

            // Crear el modelo de vista
            var model = new HistorialCompletoViewModel
            {
                Usuario = usuario,
                PuntosActuales = usuario.PuntosFidelidad ?? 0,
                TransaccionesPuntos = transacciones,
                Pedidos = pedidos,
                CanjesRecompensas = canjes
            };

            return View(model);
        }

        public async Task<IActionResult> DetallePedido(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var pedido = await _context.Pedidos
                .Include(p => p.PedidoProductos)
                    .ThenInclude(pp => pp.Producto)
                .Include(p => p.Sucursal)
                .FirstOrDefaultAsync(p => p.Id == id && p.UsuarioId == userId);

            if (pedido == null) return NotFound();

            return View(pedido);
        }

        // Método estático para calcular puntos que se ganarán
        public static int CalcularPuntosAGanar(decimal precio)
        {
            return (int)(precio * PUNTOS_POR_DOLAR);
        }

        // Nueva acción para mostrar detalle de un canje específico
        public async Task<IActionResult> DetalleCanje(string codigo)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return RedirectToAction("Acceso", "Cuentas");

            if (string.IsNullOrEmpty(codigo))
            {
                TempData["Error"] = "Código de canje no válido";
                return RedirectToAction("MisCanjes");
            }

            var usuario = await _context.AppUsuario.FindAsync(userId);
            if (usuario == null) return NotFound();

            // Obtener canjes por código (agrupados por fecha/hora)
            var canjes = await _context.HistorialCanjes
                .Include(h => h.ProductoRecompensa)
                    .ThenInclude(pr => pr.Producto)
                .Where(h => h.UsuarioId == userId)
                .OrderByDescending(h => h.FechaCanje)
                .ToListAsync();

            // Agrupar canjes y buscar el que coincida con el código
            var canjesAgrupados = canjes
                .GroupBy(c => new {
                    Fecha = c.FechaCanje.Date,
                    Hora = c.FechaCanje.Hour,
                    Minuto = c.FechaCanje.Minute
                })
                .Select(group => new {
                    CodigoCanje = GenerarCodigoCanjePorFecha(group.First().FechaCanje, group.First().Id),
                    FechaCanje = group.First().FechaCanje,
                    CanjesIndividuales = group.ToList(),
                    TotalPuntosUtilizados = group.Sum(c => c.PuntosUtilizados),
                    CantidadRecompensas = group.Count(),
                    ValorTotalAhorrado = group.Sum(c => c.ProductoRecompensa?.PrecioOriginal ?? 0)
                })
                .FirstOrDefault(c => c.CodigoCanje == codigo);

            if (canjesAgrupados == null)
            {
                TempData["Error"] = "No se encontró el canje especificado";
                return RedirectToAction("MisCanjes");
            }

            // Crear el modelo similar al ResumenCanjeMultiple
            var model = new ResumenCanjesMultiplesViewModel
            {
                Usuario = usuario,
                TotalPuntosUtilizados = canjesAgrupados.TotalPuntosUtilizados,
                CantidadRecompensas = canjesAgrupados.CantidadRecompensas,
                PuntosRestantes = usuario.PuntosFidelidad ?? 0,
                FechaCanje = canjesAgrupados.FechaCanje,
                CodigoCanje = canjesAgrupados.CodigoCanje,
                CanjesRealizados = canjesAgrupados.CanjesIndividuales,
                ValorTotalAhorrado = canjesAgrupados.ValorTotalAhorrado
            };

            return View(model);
        }
    }

}