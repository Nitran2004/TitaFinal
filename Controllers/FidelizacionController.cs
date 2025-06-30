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
        [Authorize(Roles = "Administrador,Registrado")]
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

        // Vista de carrito de recompensas
        public async Task<IActionResult> VerCarritoRecompensas()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var puntosUsuario = 0;

            if (userId != null)
            {
                var usuario = await _context.AppUsuario.FindAsync(userId);
                puntosUsuario = usuario?.PuntosFidelidad ?? 0;
            }

            // Obtener recompensas para validación
            var recompensas = await _context.ProductosRecompensa
                .Include(r => r.Producto)
                .OrderBy(r => r.PuntosNecesarios)
                .ToListAsync();

            var model = new RecompensasViewModel
            {
                PuntosUsuario = puntosUsuario,
                ProductosRecompensa = recompensas
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ActualizarTipoServicio(string codigoCanje, string tipoServicio, string canjeIds)
        {
            try
            {
                var usuarioIdentity = await _userManager.GetUserAsync(User);
                if (usuarioIdentity == null)
                {
                    return RedirectToAction("Recompensas");
                }

                var usuario = (AppUsuario)usuarioIdentity;

                // ✅ USAR IDs ESPECÍFICOS DE LOS CANJES
                List<int> idsCanjes = new List<int>();

                if (!string.IsNullOrEmpty(canjeIds))
                {
                    // Parsear los IDs que vienen del formulario
                    var idsStr = canjeIds.Split(',');
                    foreach (var idStr in idsStr)
                    {
                        if (int.TryParse(idStr.Trim(), out int id))
                        {
                            idsCanjes.Add(id);
                        }
                    }
                }

                // Si no hay IDs específicos, buscar por código (fallback al método anterior)
                if (!idsCanjes.Any())
                {
                    // Buscar canjes recientes sin tipo de servicio
                    var fechaLimite = DateTime.Now.AddHours(-2);
                    var canjesRecientes = await _context.HistorialCanjes
                        .Where(c => c.UsuarioId == usuario.Id &&
                                   c.FechaCanje >= fechaLimite &&
                                   (c.TipoServicio == null || c.TipoServicio == ""))
                        .ToListAsync();

                    idsCanjes = canjesRecientes.Select(c => c.Id).ToList();
                }

                if (!idsCanjes.Any())
                {
                    var viewModelError = new ResumenCanjesMultiplesViewModel
                    {
                        Usuario = usuario,
                        CanjesRealizados = new List<HistorialCanje>(),
                        TotalPuntosUtilizados = 0,
                        CantidadRecompensas = 0,
                        PuntosRestantes = usuario.PuntosFidelidad ?? 0,
                        FechaCanje = DateTime.Now,
                        CodigoCanje = codigoCanje,
                        ValorTotalAhorrado = 0
                    };

                    ViewBag.ErrorMessage = "No se encontraron canjes para actualizar";
                    return View("ResumenCanjeMultiple", viewModelError);
                }

                // ✅ BUSCAR Y ACTUALIZAR SOLO LOS CANJES ESPECÍFICOS
                var canjesEspecificos = await _context.HistorialCanjes
                    .Include(h => h.ProductoRecompensa)
                        .ThenInclude(pr => pr.Producto)
                    .Where(c => idsCanjes.Contains(c.Id) && c.UsuarioId == usuario.Id)
                    .ToListAsync();

                Console.WriteLine($"[DEBUG] IDs a actualizar: {string.Join(", ", idsCanjes)}");
                Console.WriteLine($"[DEBUG] Canjes encontrados: {canjesEspecificos.Count}");

                foreach (var canje in canjesEspecificos)
                {
                    canje.TipoServicio = tipoServicio;
                    Console.WriteLine($"[DEBUG] Actualizando canje ID {canje.Id} con tipo: {tipoServicio}");
                }

                await _context.SaveChangesAsync();

                var viewModel = new ResumenCanjesMultiplesViewModel
                {
                    Usuario = usuario,
                    CanjesRealizados = canjesEspecificos,
                    TotalPuntosUtilizados = canjesEspecificos.Sum(c => c.PuntosUtilizados),
                    CantidadRecompensas = canjesEspecificos.Count,
                    PuntosRestantes = usuario.PuntosFidelidad ?? 0,
                    FechaCanje = canjesEspecificos.First().FechaCanje,
                    CodigoCanje = codigoCanje,
                    ValorTotalAhorrado = canjesEspecificos.Sum(c => c.ProductoRecompensa?.PrecioOriginal ?? 0)
                };

                ViewBag.SuccessMessage = $"Tipo de servicio actualizado: {tipoServicio}";
                return View("ResumenCanjeMultiple", viewModel);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Error en ActualizarTipoServicio: {ex.Message}");
                TempData["Error"] = "Error al actualizar el tipo de servicio";
                return RedirectToAction("Recompensas");
            }
        }

        [HttpGet]
        public async Task<IActionResult> VerCanjeActualizado(string codigo)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return RedirectToAction("Acceso", "Cuentas");

            var usuario = await _context.AppUsuario.FindAsync(userId);
            if (usuario == null) return NotFound();

            // Buscar canjes recientes del usuario
            var fechaLimite = DateTime.Now.AddHours(-4); // Últimas 4 horas
            var canjesRecientes = await _context.HistorialCanjes
                .Include(h => h.ProductoRecompensa)
                    .ThenInclude(pr => pr.Producto)
                .Where(c => c.UsuarioId == userId && c.FechaCanje >= fechaLimite)
                .OrderByDescending(c => c.FechaCanje)
                .Take(10)
                .ToListAsync();

            if (!canjesRecientes.Any())
            {
                return RedirectToAction("Recompensas");
            }

            var viewModel = new ResumenCanjesMultiplesViewModel
            {
                Usuario = usuario,
                CanjesRealizados = canjesRecientes,
                TotalPuntosUtilizados = canjesRecientes.Sum(c => c.PuntosUtilizados),
                CantidadRecompensas = canjesRecientes.Count,
                PuntosRestantes = usuario.PuntosFidelidad ?? 0,
                FechaCanje = canjesRecientes.First().FechaCanje,
                CodigoCanje = codigo ?? GenerarCodigoCanjeMultiple(canjesRecientes.First().Id),
                ValorTotalAhorrado = canjesRecientes.Sum(c => c.ProductoRecompensa?.PrecioOriginal ?? 0)
            };

            return View("ResumenCanjeMultiple", viewModel);
        }

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

            // ✅ CORREGIR: GUARDAR DATOS EN EL FORMATO CORRECTO PARA RECOLECCIÓN
            var datosRecompensa = new
            {
                RecompensaId = recompensaId,
                UsuarioId = userId,
                FechaCanje = DateTime.Now,
                NombreRecompensa = productoRecompensa.Nombre,
                PuntosUtilizados = productoRecompensa.PuntosNecesarios,
                HistorialCanjeId = historialCanje.Id,
                // ✅ AGREGAR DATOS DEL PRODUCTO PARA EL PEDIDO
                ProductoId = productoRecompensa.ProductoId,
                Categoria = productoRecompensa.Categoria,
                PrecioOriginal = productoRecompensa.PrecioOriginal
            };

            TempData["RecompensaCanjeada"] = System.Text.Json.JsonSerializer.Serialize(datosRecompensa);

            // ✅ AGREGAR MENSAJE DE ÉXITO
            TempData["Success"] = $"¡Recompensa '{productoRecompensa.Nombre}' canjeada exitosamente! Ahora selecciona dónde recogerla.";

            // REDIRIGIR AL FLUJO DE RECOLECCIÓN
            return RedirectToAction("Seleccionar", "Recoleccion");
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
                    CodigoCanje = GenerarCodigoCanjeMultiple(canjesACrear.First().Id),
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
                CodigoCanje = GenerarCodigoCanjeMultiple(canjesRecientes.First().Id),
                CanjesRealizados = canjesRecientes, // ✅ Agregar los canjes
                ValorTotalAhorrado = valorTotalAhorrado // ✅ Agregar valor ahorrado
            };

            return View(model);
        }


        // Método para mostrar todos los canjes del usuario
        [Authorize(Roles = "Administrador,Registrado")]
        public async Task<IActionResult> MisCanjes()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return RedirectToAction("Acceso", "Cuentas");

            var usuario = await _context.AppUsuario.FindAsync(userId);
            if (usuario == null) return NotFound();

            // ✅ VERIFICAR SI ES ADMINISTRADOR
            bool esAdministrador = User.IsInRole("Administrador");

            List<HistorialCanje> canjes;

            if (esAdministrador)
            {
                // ✅ ADMINISTRADOR: VER TODOS LOS CANJES
                canjes = await _context.HistorialCanjes
                    .Include(h => h.ProductoRecompensa)
                        .ThenInclude(pr => pr.Producto)
                    .OrderByDescending(h => h.FechaCanje)
                    .ToListAsync();
                Console.WriteLine($"DEBUG: Administrador {userId} - Mostrando TODOS los canjes ({canjes.Count})");
            }
            else
            {
                // ✅ USUARIO NORMAL: SOLO SUS CANJES
                canjes = await _context.HistorialCanjes
                    .Include(h => h.ProductoRecompensa)
                        .ThenInclude(pr => pr.Producto)
                    .Where(h => h.UsuarioId == userId)
                    .OrderByDescending(h => h.FechaCanje)
                    .ToListAsync();
                Console.WriteLine($"DEBUG: Usuario {userId} - Mostrando solo sus canjes ({canjes.Count})");
            }

            // ✅ DEBUG: Verificar qué datos tenemos
            foreach (var canje in canjes.Take(3))
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
            foreach (var grupo in canjesAgrupados.Take(3))
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
        // ✅ MÉTODO ACTUALIZADO
        private string GenerarCodigoCanjeMultiple(int primerCanjeId)
        {
            var fecha = DateTime.Now.ToString("yyyyMMdd");
            // Usar el ID del primer canje en lugar de número aleatorio
            return $"CJM-{fecha}-{primerCanjeId}";
        }

        private string GenerarCodigoCanje(int canjeId)
        {
            // Formato: CJ-YYYYMMDD-ID (ej: CJ-20250528-00123)
            var fecha = DateTime.Now.ToString("yyyyMMdd");
            var codigo = $"CJ-{fecha}-{canjeId:D5}";
            return codigo;
        }

        public async Task<IActionResult> Historial()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return RedirectToAction("Acceso", "Cuentas");

            // Obtener usuario actual
            var usuario = await _context.AppUsuario.FindAsync(userId);
            if (usuario == null) return NotFound();

            // ✅ VERIFICAR SI ES ADMINISTRADOR O CAJERO
            bool esAdminOCajero = User.IsInRole("Administrador") || User.IsInRole("Cajero");

            // 1. Obtener transacciones de puntos - AUMENTADO A 70
            var transacciones = await _context.TransaccionesPuntos
                .Where(t => t.UsuarioId == userId)
                .OrderByDescending(t => t.Fecha)
                .Take(70)
                .ToListAsync();

            // 2. ✅ OBTENER PEDIDOS CON LÓGICA CORREGIDA
            var pedidosQuery = _context.Pedidos
                .Include(p => p.PedidoProductos)
                    .ThenInclude(pp => pp.Producto)
                .Include(p => p.Detalles)
                    .ThenInclude(d => d.Producto)
                .Include(p => p.Sucursal);

            List<Pedido> pedidos;

            if (esAdminOCajero)
            {
                // ✅ ADMIN/CAJERO: VER TODOS LOS PEDIDOS
                pedidos = await pedidosQuery
                    .OrderByDescending(p => p.Fecha)
                    .Take(50)
                    .ToListAsync();
                Console.WriteLine($"[DEBUG] Usuario {userId} (Admin/Cajero) - Mostrando TODOS los pedidos");
            }
            else
            {
                // ✅ USUARIO NORMAL: SOLO SUS PEDIDOS
                pedidos = await pedidosQuery
                    .Where(p => p.UsuarioId == userId) // SIN "|| p.UsuarioId == null"
                    .OrderByDescending(p => p.Fecha)
                    .Take(50)
                    .ToListAsync();
                Console.WriteLine($"[DEBUG] Usuario {userId} (Normal) - Mostrando solo sus pedidos");
            }

            // 3. Obtener recompensas canjeadas - AUMENTADO A 40
            var canjes = await _context.HistorialCanjes
                .Include(h => h.ProductoRecompensa)
                    .ThenInclude(pr => pr.Producto)
                .Where(h => h.UsuarioId == userId)
                .OrderByDescending(h => h.FechaCanje)
                .Take(40)
                .ToListAsync();

            Console.WriteLine($"[DEBUG] Historial - Usuario {userId} tiene {pedidos.Count} pedidos");

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
            // Verificar que el usuario esté autenticado
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Buscar el pedido y verificar que pertenezca al usuario actual
            var pedido = await _context.Pedidos
                .Include(p => p.PedidoProductos)
                    .ThenInclude(pp => pp.Producto)
                .Include(p => p.Detalles) // ✅ INCLUIR DETALLES PARA DETECTAR PERSONALIZACIÓN
                    .ThenInclude(d => d.Producto)
                .Include(p => p.Sucursal)
                .FirstOrDefaultAsync(p => p.Id == id && (p.UsuarioId == userId || p.UsuarioId == null)); // Temporal

            if (pedido == null)
            {
                TempData["Error"] = "Pedido no encontrado o no tienes permisos para verlo";
                return RedirectToAction("Historial");
            }

            // ✅ REDIRIGIR SEGÚN EL TIPO DE PEDIDO
            bool esPedidoPersonalizacion = pedido.Detalles != null && pedido.Detalles.Any();

            if (esPedidoPersonalizacion)
            {
                // Es un pedido de personalización → Redirigir a Personalizacion/Confirmacion
                Console.WriteLine($"[DEBUG] Redirigiendo a Personalizacion/Confirmacion para pedido {id}");
                return RedirectToAction("Confirmacion", "Personalizacion", new { id = id });
            }
            else
            {
                // Es un pedido normal → Redirigir a Pedidos/Resumen
                Console.WriteLine($"[DEBUG] Redirigiendo a Pedidos/Resumen para pedido {id}");
                return RedirectToAction("Resumen", "Pedidos", new { id = id });
            }
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


        [Authorize(Roles = "Administrador,Cajero")]
        public async Task<IActionResult> AdminCanjesIndex()
        {
            try
            {
                // Consulta super simple sin Include problemático
                var todosLosCanjes = await _context.HistorialCanjes.ToListAsync();

                // Obtener IDs únicos de usuarios
                var usuariosIds = todosLosCanjes.Select(c => c.UsuarioId).Distinct().ToList();

                // Obtener usuarios por separado
                var todosLosUsuarios = await _context.AppUsuario
                    .Where(u => usuariosIds.Contains(u.Id))
                    .ToListAsync();

                // Procesar en memoria para evitar problemas de EF
                var usuariosConCanjes = new List<UsuarioCanjesIndexViewModel>();

                foreach (var usuario in todosLosUsuarios)
                {
                    var canjesDelUsuario = todosLosCanjes.Where(c => c.UsuarioId == usuario.Id).ToList();

                    if (canjesDelUsuario.Any())
                    {
                        usuariosConCanjes.Add(new UsuarioCanjesIndexViewModel
                        {
                            UsuarioId = usuario.Id,
                            UserName = usuario.UserName ?? "Sin username",
                            Nombre = usuario.Nombre ?? "Sin nombre",
                            Email = usuario.Email ?? "Sin email",
                            PuntosFidelidad = usuario.PuntosFidelidad ?? 0,
                            TotalCanjes = canjesDelUsuario.Count,
                            UltimoCanje = canjesDelUsuario.Max(c => c.FechaCanje),
                            TotalPuntosUtilizados = canjesDelUsuario.Sum(c => c.PuntosUtilizados)
                        });
                    }
                }

                // Ordenar por último canje
                usuariosConCanjes = usuariosConCanjes.OrderByDescending(u => u.UltimoCanje).ToList();

                // Estadísticas
                var estadisticas = new EstadisticasGeneralesViewModel
                {
                    TotalUsuarios = usuariosConCanjes.Count,
                    TotalCanjes = usuariosConCanjes.Sum(u => u.TotalCanjes),
                    TotalPuntosUtilizados = usuariosConCanjes.Sum(u => u.TotalPuntosUtilizados),
                    UsuarioMasActivo = usuariosConCanjes.OrderByDescending(u => u.TotalCanjes).FirstOrDefault()?.Nombre ?? "N/A"
                };

                var model = new AdminCanjesIndexViewModel
                {
                    Usuarios = usuariosConCanjes,
                    Estadisticas = estadisticas
                };

                return View(model);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al cargar datos: " + ex.Message;

                // Modelo vacío en caso de error
                return View(new AdminCanjesIndexViewModel
                {
                    Usuarios = new List<UsuarioCanjesIndexViewModel>(),
                    Estadisticas = new EstadisticasGeneralesViewModel
                    {
                        TotalUsuarios = 0,
                        TotalCanjes = 0,
                        TotalPuntosUtilizados = 0,
                        UsuarioMasActivo = "N/A"
                    }
                });
            }
        }

        // AGREGAR ESTOS MÉTODOS AL FidelizacionController.cs

        [HttpPost]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> ActualizarEstadoCanje(int id, string estado)
        {
            try
            {
                var canje = await _context.HistorialCanjes.FindAsync(id);
                if (canje == null)
                {
                    TempData["Error"] = "Canje no encontrado";
                    return RedirectToAction("AdminCanjesIndex");
                }

                canje.Estado = estado;
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Estado del canje actualizado a: {estado}";

                // Obtener el usuarioId para redirigir de vuelta al detalle
                return RedirectToAction("AdminCanjesDetalle", new { usuarioId = canje.UsuarioId });
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al actualizar el estado del canje";
                return RedirectToAction("AdminCanjesIndex");
            }
        }

        [HttpPost]
        public async Task<IActionResult> GuardarComentarioCanje(int canjeId, int calificacion, string comentario)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                var canje = await _context.HistorialCanjes
                    .FirstOrDefaultAsync(c => c.Id == canjeId && c.UsuarioId == userId);

                if (canje == null)
                {
                    TempData["Error"] = "Canje no encontrado";
                    return RedirectToAction("MisCanjes");
                }

                if (canje.Estado != "Entregado")
                {
                    TempData["Error"] = "Solo puedes comentar canjes que han sido entregados";
                    return RedirectToAction("DetalleCanje", new { codigo = GenerarCodigoCanjePorFecha(canje.FechaCanje, canje.Id) });
                }

                canje.Calificacion = calificacion;
                canje.Comentario = comentario;
                canje.ComentarioEnviado = true;

                await _context.SaveChangesAsync();

                TempData["Success"] = "¡Gracias por tu valoración!";
                return RedirectToAction("DetalleCanje", new { codigo = GenerarCodigoCanjePorFecha(canje.FechaCanje, canje.Id) });
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al guardar la valoración";
                return RedirectToAction("MisCanjes");
            }
        }

        [HttpPost]
        public async Task<IActionResult> CambiarEstadoCanjeAEntregado([FromBody] dynamic request)
        {
            try
            {
                int canjeId = request.CanjeId;
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                var canje = await _context.HistorialCanjes
                    .FirstOrDefaultAsync(c => c.Id == canjeId && c.UsuarioId == userId);

                if (canje == null)
                {
                    return Json(new { success = false, message = "Canje no encontrado" });
                }

                if (canje.Estado != "Listo para entregar")
                {
                    return Json(new { success = false, message = "El canje no está listo para entregar" });
                }

                canje.Estado = "Entregado";
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Estado actualizado a Entregado" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Error al cambiar estado: {ex.Message}");
                return Json(new { success = false, message = "Error interno del servidor" });
            }
        }

        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> AdminCanjesDetalle(string usuarioId)
        {
            if (string.IsNullOrEmpty(usuarioId))
            {
                TempData["Error"] = "ID de usuario no válido";
                return RedirectToAction("AdminCanjesIndex");
            }

            try
            {
                var usuario = await _context.AppUsuario.FindAsync(usuarioId);
                if (usuario == null)
                {
                    TempData["Error"] = "Usuario no encontrado";
                    return RedirectToAction("AdminCanjesIndex");
                }

                var canjesUsuario = await _context.HistorialCanjes
                    .Where(h => h.UsuarioId == usuarioId)
                    .OrderByDescending(h => h.FechaCanje)
                    .ToListAsync();

                var productosRecompensaIds = canjesUsuario.Select(c => c.ProductoRecompensaId).Distinct().ToList();

                var productosRecompensa = await _context.ProductosRecompensa
                    .Include(pr => pr.Producto)
                    .Where(pr => productosRecompensaIds.Contains(pr.Id))
                    .ToListAsync();

                var historialCanjes = canjesUsuario.Select(h =>
                {
                    var productoRecompensa = productosRecompensa.FirstOrDefault(pr => pr.Id == h.ProductoRecompensaId);

                    return new CanjeDetalleViewModel
                    {
                        Id = h.Id,
                        ProductoRecompensaId = h.ProductoRecompensaId,
                        NombreProducto = productoRecompensa?.Nombre ?? "Producto eliminado",
                        CategoriaProducto = productoRecompensa?.Categoria ?? "N/A",
                        PuntosUtilizados = h.PuntosUtilizados,
                        FechaCanje = h.FechaCanje,
                        PrecioOriginal = productoRecompensa?.PrecioOriginal ?? 0,
                        CodigoCanje = GenerarCodigoCanjePorFecha(h.FechaCanje, h.Id),
                        Estado = h.Estado ?? "Preparándose",
                        ComentarioEnviado = h.ComentarioEnviado,
                        Calificacion = h.Calificacion,
                        Comentario = h.Comentario,
                        TipoServicio = h.TipoServicio
                    };
                }).ToList();

                var model = new AdminCanjesDetalleViewModel
                {
                    Usuario = usuario,
                    HistorialCanjes = historialCanjes,
                    TotalCanjes = canjesUsuario.Count,
                    TotalPuntosUtilizados = canjesUsuario.Sum(c => c.PuntosUtilizados),
                    TotalValorAhorrado = historialCanjes.Sum(h => h.PrecioOriginal)
                };

                return View(model);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al cargar detalle: " + ex.Message;
                return RedirectToAction("AdminCanjesIndex");
            }
        }


    }

}