using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoIdentity.Datos;
using ProyectoIdentity.Models;

namespace ProyectoIdentity.Controllers
{
    public class DiagnosticoPuntosController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DiagnosticoPuntosController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Acción para diagnosticar el problema actual
        public async Task<IActionResult> DiagnosticarPuntos()
        {
            var diagnostico = new
            {
                // Ver todas las transacciones de puntos
                TransaccionesPuntos = await _context.TransaccionesPuntos
                    .OrderByDescending(t => t.Fecha)
                    .Take(20)
                    .Select(t => new
                    {
                        t.UsuarioId,
                        t.Puntos,
                        t.Tipo,
                        t.Descripcion,
                        t.Fecha
                    })
                    .ToListAsync(),

                // Ver puntos actuales de usuarios
                UsuariosConPuntos = await _context.AppUsuario
                    .Where(u => u.PuntosFidelidad > 0)
                    .Select(u => new
                    {
                        u.Id,
                        u.Email,
                        u.PuntosFidelidad
                    })
                    .ToListAsync(),

                // Contar transacciones por tipo
                ResumenTransacciones = await _context.TransaccionesPuntos
                    .GroupBy(t => t.Tipo)
                    .Select(g => new
                    {
                        Tipo = g.Key,
                        Cantidad = g.Count(),
                        TotalPuntos = g.Sum(t => t.Puntos)
                    })
                    .ToListAsync()
            };

            return Json(diagnostico);
        }

        // Opción 1: Limpiar TODOS los datos de puntos (CUIDADO - Esto borra todo)
        [HttpPost]
        public async Task<IActionResult> LimpiarTodosPuntos()
        {
            try
            {
                // Eliminar todas las transacciones de puntos
                var transacciones = await _context.TransaccionesPuntos.ToListAsync();
                _context.TransaccionesPuntos.RemoveRange(transacciones);

                // Eliminar todos los historiales de canje
                var historiales = await _context.HistorialCanjes.ToListAsync();
                _context.HistorialCanjes.RemoveRange(historiales);

                // Resetear puntos de todos los usuarios a 0
                var usuarios = await _context.AppUsuario.Where(u => u.PuntosFidelidad > 0).ToListAsync();
                foreach (var usuario in usuarios)
                {
                    usuario.PuntosFidelidad = 0;
                }

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Todos los puntos han sido limpiados exitosamente" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        // Opción 2: Convertir puntos existentes de escala 175 a escala 30
        [HttpPost]
        public async Task<IActionResult> ConvertirPuntosANuevaEscala()
        {
            try
            {
                const decimal FACTOR_CONVERSION = 30m / 175m; // 0.1714...

                // Convertir puntos de usuarios
                var usuarios = await _context.AppUsuario.Where(u => u.PuntosFidelidad > 0).ToListAsync();
                foreach (var usuario in usuarios)
                {
                    var puntosOriginales = usuario.PuntosFidelidad ?? 0;
                    usuario.PuntosFidelidad = (int)(puntosOriginales * FACTOR_CONVERSION);
                }

                // Convertir transacciones de puntos
                var transacciones = await _context.TransaccionesPuntos.ToListAsync();
                foreach (var transaccion in transacciones)
                {
                    var puntosOriginales = transaccion.Puntos;
                    transaccion.Puntos = (int)(puntosOriginales * FACTOR_CONVERSION);

                    // Actualizar descripción para indicar conversión
                    if (!transaccion.Descripcion.Contains("(convertido)"))
                    {
                        transaccion.Descripcion += " (convertido a nueva escala)";
                    }
                }

                // Convertir historial de canjes
                var canjes = await _context.HistorialCanjes.ToListAsync();
                foreach (var canje in canjes)
                {
                    canje.PuntosUtilizados = (int)(canje.PuntosUtilizados * FACTOR_CONVERSION);
                }

                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = $"Conversión completada. Factor usado: {FACTOR_CONVERSION:F4}",
                    usuariosAfectados = usuarios.Count,
                    transaccionesAfectadas = transacciones.Count,
                    canjesAfectados = canjes.Count
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        // Opción 3: Crear transacción de ajuste para un usuario específico
        [HttpPost]
        public async Task<IActionResult> AjustarPuntosUsuario(string usuarioId, int nuevoPuntaje)
        {
            try
            {
                var usuario = await _context.AppUsuario.FindAsync(usuarioId);
                if (usuario == null)
                    return Json(new { success = false, message = "Usuario no encontrado" });

                var puntosActuales = usuario.PuntosFidelidad ?? 0;
                var diferencia = nuevoPuntaje - puntosActuales;

                // Actualizar puntos del usuario
                usuario.PuntosFidelidad = nuevoPuntaje;

                // Crear transacción de ajuste
                var transaccionAjuste = new TransaccionPuntos
                {
                    UsuarioId = usuarioId,
                    Puntos = diferencia,
                    Tipo = "Ajuste",
                    Descripcion = $"Ajuste manual de puntos. Anterior: {puntosActuales}, Nuevo: {nuevoPuntaje}",
                    Fecha = DateTime.Now
                };

                _context.TransaccionesPuntos.Add(transaccionAjuste);
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = $"Puntos ajustados exitosamente",
                    puntosAnteriores = puntosActuales,
                    puntosNuevos = nuevoPuntaje,
                    diferencia = diferencia
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        // Vista principal para gestionar el diagnóstico
        public IActionResult Index()
        {
            return View();
        }
    }
}