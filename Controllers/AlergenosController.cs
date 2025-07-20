using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoIdentity.Datos;
using ProyectoIdentity.Models;
using System.Security.Claims;

namespace ProyectoIdentity.Controllers
{
    [Authorize] // Solo usuarios autenticados pueden acceder
    public class AlergenosController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AlergenosController> _logger;

        public AlergenosController(ApplicationDbContext context, ILogger<AlergenosController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: /Alergenos/Gestion
        public async Task<IActionResult> Gestion()
        {
            var usuarioId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(usuarioId))
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                // Obtener información del usuario
                var usuario = await _context.Users.FindAsync(usuarioId);
                if (usuario == null)
                {
                    return NotFound("Usuario no encontrado");
                }

                // Obtener todos los tipos de alérgenos disponibles
                var tiposAlergenos = await _context.TiposAlergenos
                    .Where(ta => ta.Activo)
                    .OrderBy(ta => ta.Nombre)
                    .ToListAsync();

                // Obtener alérgenos actuales del usuario
                var alergenosUsuario = await _context.UsuarioAlergenos
                    .Include(ua => ua.TipoAlergeno)
                    .Where(ua => ua.UsuarioId == usuarioId)
                    .ToListAsync();

                var viewModel = new GestionAlergenosViewModel
                {
                    UsuarioId = usuarioId,
                    NombreUsuario = usuario.UserName ?? "Usuario",
                    TiposAlergenosDisponibles = tiposAlergenos,
                    AlergenosUsuario = alergenosUsuario,
                    AlergenosSeleccionados = alergenosUsuario.Select(au => au.TipoAlergenoId).ToList()
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar la gestión de alérgenos para el usuario {UsuarioId}", usuarioId);
                TempData["Error"] = "Error al cargar la información de alérgenos.";
                return RedirectToAction("Index", "Home");
            }
        }

        // POST: /Alergenos/ActualizarAlergenos
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ActualizarAlergenos(ActualizarAlergenosDto model)
        {
            var usuarioId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(usuarioId))
            {
                return Json(new { success = false, message = "Usuario no autenticado" });
            }

            try
            {
                // Eliminar alérgenos existentes del usuario
                var alergenosExistentes = await _context.UsuarioAlergenos
                    .Where(ua => ua.UsuarioId == usuarioId)
                    .ToListAsync();

                _context.UsuarioAlergenos.RemoveRange(alergenosExistentes);

                // Agregar nuevos alérgenos seleccionados
                if (model.TiposAlergenosIds != null && model.TiposAlergenosIds.Any())
                {
                    var nuevosAlergenos = model.TiposAlergenosIds.Select(tipoId => new UsuarioAlergeno
                    {
                        UsuarioId = usuarioId,
                        TipoAlergenoId = tipoId,
                        FechaRegistro = DateTime.Now,
                        Notas = model.NotasGenerales
                    }).ToList();

                    await _context.UsuarioAlergenos.AddRangeAsync(nuevosAlergenos);
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Alérgenos actualizados para el usuario {UsuarioId}. Total: {Count}",
                    usuarioId, model.TiposAlergenosIds?.Count ?? 0);

                TempData["Success"] = "Tus alérgenos han sido actualizados correctamente.";
                return Json(new { success = true, message = "Alérgenos actualizados correctamente" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar alérgenos del usuario {UsuarioId}", usuarioId);
                return Json(new { success = false, message = "Error al actualizar los alérgenos" });
            }
        }

        // GET: /Alergenos/Recomendaciones
        public async Task<IActionResult> Recomendaciones()
        {
            var usuarioId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(usuarioId))
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                // Obtener información del usuario
                var usuario = await _context.Users.FindAsync(usuarioId);
                if (usuario == null)
                {
                    return NotFound("Usuario no encontrado");
                }

                // Obtener alérgenos del usuario
                var alergenosUsuario = await _context.UsuarioAlergenos
                    .Include(ua => ua.TipoAlergeno)
                    .Where(ua => ua.UsuarioId == usuarioId)
                    .Select(ua => ua.TipoAlergeno.Nombre)
                    .ToListAsync();

                // Obtener todos los productos disponibles
                var todosLosProductos = await _context.Productos
                    .Where(p => p.Cantidad > 0)
                    .ToListAsync();

                // Filtrar productos según alérgenos
                var (productosRecomendados, productosNoRecomendados) = FiltrarProductosPorAlergenos(todosLosProductos, alergenosUsuario);

                // Crear mensaje de recomendación
                var mensajeRecomendacion = CrearMensajeRecomendacion(alergenosUsuario, productosRecomendados.Count, productosNoRecomendados.Count);

                var viewModel = new RecomendacionAlergenosViewModel
                {
                    NombreUsuario = usuario.UserName ?? "Usuario",
                    AlergenosUsuario = alergenosUsuario,
                    ProductosRecomendados = productosRecomendados,
                    ProductosNoRecomendados = productosNoRecomendados,
                    TotalProductosAnalizados = todosLosProductos.Count,
                    MensajeRecomendacion = mensajeRecomendacion
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar recomendaciones para el usuario {UsuarioId}", usuarioId);
                TempData["Error"] = "Error al generar las recomendaciones.";
                return RedirectToAction("Gestion");
            }
        }

        // GET: /Alergenos/MisAlergenos (API para obtener alérgenos del usuario actual)
        [HttpGet]
        public async Task<IActionResult> MisAlergenos()
        {
            var usuarioId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(usuarioId))
            {
                return Json(new { success = false, message = "Usuario no autenticado" });
            }

            try
            {
                var alergenosUsuario = await _context.UsuarioAlergenos
                    .Include(ua => ua.TipoAlergeno)
                    .Where(ua => ua.UsuarioId == usuarioId)
                    .Select(ua => new
                    {
                        id = ua.TipoAlergeno.Id,
                        nombre = ua.TipoAlergeno.Nombre,
                        descripcion = ua.TipoAlergeno.Descripcion,
                        fechaRegistro = ua.FechaRegistro,
                        notas = ua.Notas
                    })
                    .ToListAsync();

                return Json(new { success = true, alergenos = alergenosUsuario });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener alérgenos del usuario {UsuarioId}", usuarioId);
                return Json(new { success = false, message = "Error al obtener los alérgenos" });
            }
        }

        // Método privado para filtrar productos según alérgenos del usuario
        private (List<Producto> recomendados, List<Producto> noRecomendados) FiltrarProductosPorAlergenos(
            List<Producto> productos, List<string> alergenosUsuario)
        {
            var productosRecomendados = new List<Producto>();
            var productosNoRecomendados = new List<Producto>();

            foreach (var producto in productos)
            {
                var esSeguro = true;

                if (!string.IsNullOrEmpty(producto.Alergenos) && producto.Alergenos != "NULL")
                {
                    foreach (var alergenoUsuario in alergenosUsuario)
                    {
                        if (producto.Alergenos.Contains(alergenoUsuario, StringComparison.OrdinalIgnoreCase))
                        {
                            esSeguro = false;
                            break;
                        }
                    }
                }

                // También verificar en ingredientes si existe información
                if (esSeguro && !string.IsNullOrEmpty(producto.Ingredientes) && producto.Ingredientes != "NULL")
                {
                    foreach (var alergenoUsuario in alergenosUsuario)
                    {
                        if (producto.Ingredientes.Contains(alergenoUsuario, StringComparison.OrdinalIgnoreCase))
                        {
                            esSeguro = false;
                            break;
                        }
                    }
                }

                if (esSeguro)
                {
                    productosRecomendados.Add(producto);
                }
                else
                {
                    productosNoRecomendados.Add(producto);
                }
            }

            return (productosRecomendados.OrderBy(p => p.Categoria).ThenBy(p => p.Nombre).ToList(),
                    productosNoRecomendados.OrderBy(p => p.Categoria).ThenBy(p => p.Nombre).ToList());
        }

        // Método privado para crear mensaje de recomendación
        private string CrearMensajeRecomendacion(List<string> alergenos, int productosRecomendados, int productosNoRecomendados)
        {
            if (!alergenos.Any())
            {
                return "No tienes alérgenos registrados. Puedes disfrutar de todos nuestros productos.";
            }

            var alergenosTexto = string.Join(", ", alergenos);
            var mensaje = $"Basándome en tus alérgenos ({alergenosTexto}), ";

            if (productosRecomendados > 0)
            {
                mensaje += $"encontré {productosRecomendados} productos seguros para ti. ";
            }

            if (productosNoRecomendados > 0)
            {
                mensaje += $"Te recomiendo evitar {productosNoRecomendados} productos que podrían contener alérgenos.";
            }

            return mensaje;
        }
    }
}