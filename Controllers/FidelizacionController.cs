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
        private const decimal RATIO_ESTANDAR_PUNTOS_DOLLAR = 175m;


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
        [Authorize(Roles = "Administrador,Cajero")]
        public async Task<IActionResult> ActualizarEstadoCanje(string codigoCanje, string estado, string usuarioId)
        {
            try
            {
                // ✅ BUSCAR TODOS LOS CANJES DEL MISMO CÓDIGO (MISMA TRANSACCIÓN)
                var fechaLimite = DateTime.Now.AddDays(-30); // Últimos 30 días

                var todosLosCanjes = await _context.HistorialCanjes
                    .Where(h => h.UsuarioId == usuarioId && h.FechaCanje >= fechaLimite)
                    .ToListAsync();

                // Agrupar por fecha/hora y encontrar el grupo que corresponde al código
                var canjesDelCodigo = todosLosCanjes
                    .GroupBy(h => new {
                        Fecha = h.FechaCanje.Date,
                        Hora = h.FechaCanje.Hour,
                        Minuto = h.FechaCanje.Minute
                    })
                    .FirstOrDefault(grupo => {
                        var primerCanje = grupo.First();
                        var codigoGrupo = GenerarCodigoCanjePorFecha(primerCanje.FechaCanje, primerCanje.Id);
                        return codigoGrupo == codigoCanje;
                    });

                if (canjesDelCodigo == null || !canjesDelCodigo.Any())
                {
                    TempData["Error"] = "No se encontraron canjes para este código";
                    return RedirectToAction("AdminCanjesDetalle", new { usuarioId = usuarioId });
                }

                // ✅ ACTUALIZAR EL ESTADO DE TODOS LOS CANJES DEL GRUPO
                foreach (var canje in canjesDelCodigo)
                {
                    canje.Estado = estado;
                }

                await _context.SaveChangesAsync();

                TempData["Success"] = $"Estado actualizado a '{estado}' para {canjesDelCodigo.Count()} recompensas del código {codigoCanje}";
                return RedirectToAction("AdminCanjesDetalle", new { usuarioId = usuarioId });
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al actualizar el estado del canje";
                return RedirectToAction("AdminCanjesDetalle", new { usuarioId = usuarioId });
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

        [Authorize(Roles = "Administrador,Cajero")]
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

                // ✅ AGRUPAR CANJES POR FECHA Y HORA (TRANSACCIONES DEL MISMO MOMENTO)
                var canjesAgrupados = canjesUsuario
                    .GroupBy(h => new {
                        Fecha = h.FechaCanje.Date,
                        Hora = h.FechaCanje.Hour,
                        Minuto = h.FechaCanje.Minute
                    })
                    .Select(grupo => {
                        var canjesDelGrupo = grupo.ToList();
                        var primerCanje = canjesDelGrupo.First();

                        return new CanjeAgrupadoAdminViewModel
                        {
                            CodigoCanje = GenerarCodigoCanjePorFecha(primerCanje.FechaCanje, primerCanje.Id),
                            FechaCanje = primerCanje.FechaCanje,
                            TipoServicio = primerCanje.TipoServicio,
                            Estado = primerCanje.Estado ?? "Preparándose", // Todos deben tener el mismo estado
                            TotalPuntosUtilizados = canjesDelGrupo.Sum(c => c.PuntosUtilizados),
                            CantidadRecompensas = canjesDelGrupo.Count,
                            CanjesIndividuales = canjesDelGrupo.Select(c => {
                                var productoRecompensa = productosRecompensa.FirstOrDefault(pr => pr.Id == c.ProductoRecompensaId);
                                return new CanjeDetalleViewModel
                                {
                                    Id = c.Id,
                                    ProductoRecompensaId = c.ProductoRecompensaId,
                                    NombreProducto = productoRecompensa?.Nombre ?? "Producto eliminado",
                                    CategoriaProducto = productoRecompensa?.Categoria ?? "N/A",
                                    PuntosUtilizados = c.PuntosUtilizados,
                                    FechaCanje = c.FechaCanje,
                                    PrecioOriginal = productoRecompensa?.PrecioOriginal ?? 0,
                                    Estado = c.Estado ?? "Preparándose",
                                    TipoServicio = c.TipoServicio,
                                    ComentarioEnviado = c.ComentarioEnviado,
                                    Calificacion = c.Calificacion,
                                    Comentario = c.Comentario
                                };
                            }).ToList(),
                            ValorTotalAhorrado = canjesDelGrupo.Sum(c => {
                                var pr = productosRecompensa.FirstOrDefault(p => p.Id == c.ProductoRecompensaId);
                                return pr?.PrecioOriginal ?? 0;
                            })
                        };
                    })
                    .OrderByDescending(g => g.FechaCanje)
                    .ToList();

                var model = new AdminCanjesDetalleAgrupadoViewModel
                {
                    Usuario = usuario,
                    CanjesAgrupados = canjesAgrupados,
                    TotalCanjes = canjesUsuario.Count,
                    TotalPuntosUtilizados = canjesUsuario.Sum(c => c.PuntosUtilizados),
                    TotalValorAhorrado = canjesAgrupados.Sum(g => g.ValorTotalAhorrado)
                };

                return View(model);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al cargar detalle: " + ex.Message;
                return RedirectToAction("AdminCanjesIndex");
            }
        }

        // ============== CRUD RECOMPENSAS - SOLO ADMINISTRADORES ==============

        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> AdminRecompensas()
        {
            var recompensasActuales = await _context.ProductosRecompensa
                .Include(r => r.Producto)
                .OrderBy(r => r.PuntosNecesarios)
                .ToListAsync();

            // Productos que NO están como recompensas
            var idsProductosEnRecompensas = recompensasActuales
                .Where(r => r.ProductoId.HasValue)
                .Select(r => r.ProductoId.Value)
                .ToList();

            var productosDisponibles = await _context.Productos
                .Where(p => !idsProductosEnRecompensas.Contains(p.Id))
                .OrderBy(p => p.Categoria)
                .ThenBy(p => p.Nombre)
                .ToListAsync();

            var model = new AdminRecompensasViewModel
            {
                RecompensasActuales = recompensasActuales,
                ProductosDisponibles = productosDisponibles,
                TotalRecompensas = recompensasActuales.Count,
                ValorTotalRecompensas = recompensasActuales.Sum(r => r.PrecioOriginal)
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> CrearRecompensa()
        {
            try
            {
                // Obtener todos los productos disponibles
                var productos = await _context.Productos
                    .OrderBy(p => p.Categoria)
                    .ThenBy(p => p.Nombre)
                    .ToListAsync(); // ← QUITAR .Select(), usar objetos completos

                // Debug: Verificar que se obtengan productos
                Console.WriteLine($"[DEBUG] Productos obtenidos: {productos.Count}");
                foreach (var p in productos.Take(3))
                {
                    Console.WriteLine($"[DEBUG] Producto: {p.Nombre} - ${p.Precio} - {p.Categoria}");
                }

                // CAMBIAR ESTAS LÍNEAS:
                // ViewBag.Productos = productos;
                // var model = new ProductoRecompensa();

                // POR ESTAS:
                var model = new CrearRecompensaViewModel
                {
                    ProductosDisponibles = productos
                };

                return View(model);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Error en CrearRecompensa GET: {ex.Message}");
                TempData["Error"] = "Error al cargar la página de crear recompensa";
                return RedirectToAction("AdminRecompensas");
            }
        }

        [HttpPost]
        [Authorize(Roles = "Administrador")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearRecompensa(ProductoRecompensa model)
        {
            try
            {
                Console.WriteLine($"[DEBUG] Creando recompensa: {model.Nombre}");
                Console.WriteLine($"[DEBUG] ProductoId: {model.ProductoId}");
                Console.WriteLine($"[DEBUG] PuntosNecesarios: {model.PuntosNecesarios}");
                Console.WriteLine($"[DEBUG] PrecioOriginal: {model.PrecioOriginal}");

                // Validaciones del modelo
                if (ModelState.IsValid)
                {
                    // Verificar que el producto existe
                    var productoExiste = await _context.Productos
                        .AnyAsync(p => p.Id == model.ProductoId);

                    if (!productoExiste)
                    {
                        ModelState.AddModelError("ProductoId", "El producto seleccionado no existe");
                    }

                    // Verificar que no exista ya una recompensa para este producto
                    var recompensaExiste = await _context.ProductosRecompensa
                        .AnyAsync(pr => pr.ProductoId == model.ProductoId);

                    if (recompensaExiste)
                    {
                        ModelState.AddModelError("ProductoId", "Ya existe una recompensa para este producto");
                    }

                    // Validaciones de negocio
                    if (model.PuntosNecesarios <= 0)
                    {
                        ModelState.AddModelError("PuntosNecesarios", "Los puntos necesarios deben ser mayor a 0");
                    }

                    if (model.PrecioOriginal <= 0)
                    {
                        ModelState.AddModelError("PrecioOriginal", "El precio original debe ser mayor a 0");
                    }

                    // Si todo está válido, crear la recompensa
                    if (ModelState.IsValid)
                    {
                        // Agregar a la base de datos
                        _context.ProductosRecompensa.Add(model);
                        await _context.SaveChangesAsync();

                        Console.WriteLine($"[DEBUG] ✅ Recompensa creada exitosamente con ID: {model.Id}");

                        TempData["Success"] = $"Recompensa '{model.Nombre}' creada exitosamente";
                        return RedirectToAction("AdminRecompensas");
                    }
                }

                // Si llegamos aquí, hay errores de validación
                Console.WriteLine("[DEBUG] ❌ Errores de validación:");
                foreach (var error in ModelState)
                {
                    Console.WriteLine($"[DEBUG] Campo {error.Key}: {string.Join(", ", error.Value.Errors.Select(e => e.ErrorMessage))}");
                }

                // Recargar productos para el formulario
                await CargarProductosParaFormulario();

                return View(model);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Error en CrearRecompensa POST: {ex.Message}");
                Console.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");

                TempData["Error"] = "Error al crear la recompensa: " + ex.Message;

                // Recargar productos para el formulario
                await CargarProductosParaFormulario();

                return View(model);
            }
        }

        private async Task CargarProductosParaFormulario()
        {
            try
            {
                var productos = await _context.Productos
                    .OrderBy(p => p.Categoria)
                    .ThenBy(p => p.Nombre)
                    .Select(p => new {
                        p.Id,
                        p.Nombre,
                        p.Precio,
                        p.Categoria
                    })
                    .ToListAsync();

                ViewBag.Productos = productos;
                Console.WriteLine($"[DEBUG] Productos recargados: {productos.Count}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Error al cargar productos: {ex.Message}");
                ViewBag.Productos = new List<object>();
            }
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerProductos()
        {
            try
            {
                var productos = await _context.Productos
                    .OrderBy(p => p.Categoria)
                    .ThenBy(p => p.Nombre)
                    .Select(p => new {
                        id = p.Id,
                        nombre = p.Nombre,
                        precio = p.Precio,
                        categoria = p.Categoria
                    })
                    .ToListAsync();

                return Json(new { success = true, productos = productos });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Error en ObtenerProductos: {ex.Message}");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> EditarRecompensa(int id)
        {
            var recompensa = await _context.ProductosRecompensa
                .Include(r => r.Producto)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (recompensa == null)
            {
                TempData["Error"] = "Recompensa no encontrada";
                return RedirectToAction("AdminRecompensas");
            }

            // Productos disponibles + el producto actual de la recompensa
            var idsProductosEnRecompensas = await _context.ProductosRecompensa
                .Where(r => r.ProductoId.HasValue && r.Id != id) // Excluir la recompensa actual
                .Select(r => r.ProductoId.Value)
                .ToListAsync();

            var productosDisponibles = await _context.Productos
                .Where(p => !idsProductosEnRecompensas.Contains(p.Id))
                .OrderBy(p => p.Categoria)
                .ThenBy(p => p.Nombre)
                .ToListAsync();

            var model = new EditarRecompensaViewModel
            {
                Id = recompensa.Id,
                ProductoId = recompensa.ProductoId,
                Nombre = recompensa.Nombre,
                PrecioOriginal = recompensa.PrecioOriginal,
                PuntosNecesarios = recompensa.PuntosNecesarios,
                Categoria = recompensa.Categoria,
                ImagenExistente = recompensa.Imagen,
                ProductosDisponibles = productosDisponibles
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ValidarRecompensa([FromBody] ValidarRecompensaRequest request)
        {
            try
            {
                var errores = new List<string>();

                // Verificar que el producto existe
                var producto = await _context.Productos.FindAsync(request.ProductoId);
                if (producto == null)
                {
                    errores.Add("El producto seleccionado no existe");
                }

                // Verificar que no exista ya una recompensa para este producto
                var recompensaExiste = await _context.ProductosRecompensa
                    .AnyAsync(pr => pr.ProductoId == request.ProductoId);

                if (recompensaExiste)
                {
                    errores.Add("Ya existe una recompensa para este producto");
                }

                // Validar puntos
                if (request.PuntosNecesarios <= 0)
                {
                    errores.Add("Los puntos necesarios deben ser mayor a 0");
                }

                // Calcular ratio y validar
                if (request.PrecioOriginal > 0 && request.PuntosNecesarios > 0)
                {
                    var ratio = request.PuntosNecesarios / request.PrecioOriginal;
                    if (ratio < 50)
                    {
                        errores.Add("El ratio puntos/precio es muy bajo (mínimo recomendado: 50 pts/$)");
                    }
                    else if (ratio > 300)
                    {
                        errores.Add("El ratio puntos/precio es muy alto (máximo recomendado: 300 pts/$)");
                    }
                }

                return Json(new
                {
                    valido = !errores.Any(),
                    errores = errores,
                    ratio = request.PrecioOriginal > 0 ? Math.Round(request.PuntosNecesarios / request.PrecioOriginal, 1) : 0
                });
            }
            catch (Exception ex)
            {
                return Json(new { valido = false, errores = new[] { ex.Message } });
            }
        }


        [HttpPost]
        [Authorize(Roles = "Administrador")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarRecompensa(EditarRecompensaViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var recompensa = await _context.ProductosRecompensa.FindAsync(model.Id);
                    if (recompensa == null)
                    {
                        TempData["Error"] = "Recompensa no encontrada";
                        return RedirectToAction("AdminRecompensas");
                    }

                    // Si cambió el producto, verificar que no esté ya como recompensa
                    if (model.ProductoId.HasValue && model.ProductoId != recompensa.ProductoId)
                    {
                        var yaExiste = await _context.ProductosRecompensa
                            .AnyAsync(r => r.ProductoId == model.ProductoId && r.Id != model.Id);

                        if (yaExiste)
                        {
                            TempData["Error"] = "Este producto ya está configurado como recompensa";
                            return RedirectToAction("AdminRecompensas");
                        }

                        // Actualizar imagen del nuevo producto
                        if (model.ProductoId.HasValue)
                        {
                            var producto = await _context.Productos.FindAsync(model.ProductoId.Value);
                            if (producto?.Imagen != null)
                            {
                                recompensa.Imagen = producto.Imagen;
                            }
                        }
                    }

                    recompensa.ProductoId = model.ProductoId;
                    recompensa.Nombre = model.Nombre;
                    recompensa.PrecioOriginal = model.PrecioOriginal;
                    recompensa.PuntosNecesarios = model.PuntosNecesarios;
                    recompensa.Categoria = model.Categoria;

                    _context.Update(recompensa);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Recompensa actualizada exitosamente";
                    return RedirectToAction("AdminRecompensas");
                }
                catch (Exception ex)
                {
                    TempData["Error"] = "Error al actualizar la recompensa: " + ex.Message;
                }
            }

            return RedirectToAction("AdminRecompensas");
        }

        [HttpPost]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> EliminarRecompensa([FromBody] EliminarRecompensaRequest request)
        {
            try
            {
                var recompensa = await _context.ProductosRecompensa.FindAsync(request.Id);
                if (recompensa == null)
                {
                    return Json(new { success = false, message = "Recompensa no encontrada" });
                }

                // Verificar si la recompensa tiene canjes asociados
                var tieneCanjes = await _context.HistorialCanjes
                    .AnyAsync(h => h.ProductoRecompensaId == recompensa.Id);

                if (tieneCanjes)
                {
                    return Json(new
                    {
                        success = false,
                        message = "No se puede eliminar la recompensa porque tiene canjes asociados"
                    });
                }

                _context.ProductosRecompensa.Remove(recompensa);
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = $"Recompensa '{recompensa.Nombre}' eliminada exitosamente"
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Error interno del servidor al eliminar la recompensa"
                });
            }
        }

        // Método para obtener datos del producto vía AJAX
        [HttpGet]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> ObtenerDatosProducto(int productoId)
        {
            var producto = await _context.Productos.FindAsync(productoId);
            if (producto == null)
            {
                return Json(new { success = false });
            }

            var ratioCategoria = ObtenerRatioPorCategoria(producto.Categoria);
            var puntosRecomendados = CalcularPuntosRecomendados(producto.Precio, producto.Categoria);

            return Json(new
            {
                success = true,
                nombre = producto.Nombre,
                precio = producto.Precio,
                categoria = producto.Categoria,
                puntosRecomendados = puntosRecomendados,
                ratioCategoria = ratioCategoria,
                explicacion = $"Ratio para {producto.Categoria}: {ratioCategoria} pts/$"
            });
        }

        private static readonly Dictionary<string, decimal> RatiosPorCategoria = new()
    {
        { "Sánduches", 160m },      // Más generoso para comida básica
        { "Shot", 167m },           // Shots accesibles
        { "Cocteles", 167m },       // Misma accesibilidad que shots
        { "Picadas", 170m },        // Para compartir
        { "Pizza", 175m },          // Plato principal
        { "Cerveza", 175m },        // Bebida popular
        { "Bebidas", 185m },        // Bebidas normales
        { "Promo", 200m }           // Promociones especiales (menos generoso)
    };

        private const decimal RATIO_DEFAULT = 175m;

        private decimal ObtenerRatioPorCategoria(string categoria)
        {
            if (string.IsNullOrEmpty(categoria))
                return RATIO_DEFAULT;

            // Buscar coincidencia exacta
            if (RatiosPorCategoria.ContainsKey(categoria))
                return RatiosPorCategoria[categoria];

            // Buscar coincidencia parcial (case-insensitive)
            var categoriaKey = RatiosPorCategoria.Keys
                .FirstOrDefault(k => k.Equals(categoria, StringComparison.OrdinalIgnoreCase));

            return categoriaKey != null ? RatiosPorCategoria[categoriaKey] : RATIO_DEFAULT;
        }

        private int CalcularPuntosRecomendados(decimal precio, string categoria)
        {
            var ratio = ObtenerRatioPorCategoria(categoria);
            return (int)Math.Round(precio * ratio);
        }

        private (bool esValido, string mensaje) ValidarCoherenciaCategoria(decimal precio, int puntos, string categoria)
        {
            var ratioEsperado = ObtenerRatioPorCategoria(categoria);
            var ratioActual = precio > 0 ? puntos / precio : 0;
            var diferenciaPorcentaje = Math.Abs((ratioActual - ratioEsperado) / ratioEsperado) * 100;

            if (diferenciaPorcentaje <= 15) // Tolerancia del 15%
            {
                return (true, "Coherente con el sistema");
            }
            else if (diferenciaPorcentaje <= 30) // Tolerancia del 30%
            {
                return (true, $"Aceptable. Diferencia del {diferenciaPorcentaje:F1}% respecto al estándar de {categoria} ({ratioEsperado} pts/$)");
            }
            else
            {
                return (false, $"Se aleja mucho del estándar de {categoria}. Esperado: ~{ratioEsperado} pts/$, Actual: {ratioActual:F0} pts/$");
            }
        }

        [HttpGet]
        [Authorize(Roles = "Administrador")]
        public IActionResult ObtenerRatiosPorCategoria()
        {
            var ratios = RatiosPorCategoria.Select(kvp => new
            {
                categoria = kvp.Key,
                ratio = kvp.Value,
                descripcion = GetDescripcionRatio(kvp.Value) // Cambiar nombre del método
            }).ToList();

            return Json(new { success = true, ratios = ratios });
        }

        // AGREGAR ESTE MÉTODO JUSTO DESPUÉS
        private string GetDescripcionRatio(decimal ratio)
        {
            return ratio switch
            {
                <= 165m => "Muy generoso",
                <= 175m => "Generoso",
                <= 185m => "Estándar",
                <= 195m => "Moderado",
                _ => "Costoso"
            };
        }

        // Clase para el request de eliminación
        public class EliminarRecompensaRequest
        {
            public int Id { get; set; }
        }

        public class ValidarRecompensaRequest
        {
            public int ProductoId { get; set; }
            public int PuntosNecesarios { get; set; }
            public decimal PrecioOriginal { get; set; }
            public string Nombre { get; set; }
        }
    }

}