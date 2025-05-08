using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoIdentity.Datos;  // Ajusta este namespace según tu proyecto
using ProyectoIdentity.Models;  // Ajusta este namespace según tu proyecto
using ProyectoIdentity.ViewModels;  // Ajusta este namespace según tu proyecto

namespace ProyectoIdentity.Controllers  // Ajusta este namespace según tu proyecto
{
    [Authorize]
    public class RecompensasController : Controller
    {
        private readonly ApplicationDbContext _context;

        public RecompensasController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Recompensas
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Obtener puntos del usuario
            var usuarioPuntos = await _context.UsuarioPuntos
                .FirstOrDefaultAsync(u => u.UsuarioId == userId);

            if (usuarioPuntos == null)
            {
                // Si el usuario no tiene registro de puntos, crearlo
                usuarioPuntos = new UsuarioPuntos
                {
                    UsuarioId = userId,
                    PuntosAcumulados = 0
                };
                _context.UsuarioPuntos.Add(usuarioPuntos);
                await _context.SaveChangesAsync();
            }

            // Obtener productos de recompensa
            var productosRecompensa = await _context.ProductosRecompensa.ToListAsync();

            // Obtener historial de canjes
            var historialCanjes = await _context.HistorialCanjes
                .Where(h => h.UsuarioId == userId)
                .OrderByDescending(h => h.FechaCanje)
                .Take(10)
                .Include(h => h.ProductoRecompensa)
                .Select(h => new HistorialCanjeViewModel
                {
                    FechaCanje = h.FechaCanje,
                    NombreProducto = h.ProductoRecompensa.Nombre,
                    PuntosCanjeados = h.PuntosCanjeados
                })
                .ToListAsync();

            // Obtener categorías únicas
            var categorias = productosRecompensa
                .Select(p => p.Categoria)
                .Distinct()
                .OrderBy(c => c)
                .ToList();

            // Calcular el máximo de puntos necesarios
            var puntosMaximos = productosRecompensa.Any()
                ? productosRecompensa.Max(p => p.PuntosNecesarios)
                : 2000; // Valor por defecto

            // Definir niveles de recompensa
            var nivelesRecompensa = new List<NivelRecompensa>
            {
                new NivelRecompensa { Nombre = "Bronce", PuntosNecesarios = 500 },
                new NivelRecompensa { Nombre = "Plata", PuntosNecesarios = 1000 },
                new NivelRecompensa { Nombre = "Oro", PuntosNecesarios = 1500 },
                new NivelRecompensa { Nombre = "Platino", PuntosNecesarios = 2000 }
            };

            // Crear el ViewModel
            var viewModel = new RecompensasViewModel
            {
                PuntosActuales = usuarioPuntos.PuntosAcumulados,
                PuntosMaximos = puntosMaximos,
                PorcentajeProgreso = Math.Min(100, (decimal)usuarioPuntos.PuntosAcumulados * 100 / puntosMaximos),
                ProductosRecompensa = productosRecompensa,
                NivelesRecompensa = nivelesRecompensa,
                Categorias = categorias,
                HistorialCanjes = historialCanjes
            };

            return View(viewModel);
        }

        // POST: Recompensas/CanjearRecompensa
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CanjearRecompensa(int productoId, int puntos)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Obtener usuario y sus puntos
            var usuarioPuntos = await _context.UsuarioPuntos
                .FirstOrDefaultAsync(u => u.UsuarioId == userId);

            if (usuarioPuntos == null || usuarioPuntos.PuntosAcumulados < puntos)
            {
                return Json(new { success = false, message = "No tienes suficientes puntos para este canje." });
            }

            // Obtener producto recompensa
            var productoRecompensa = await _context.ProductosRecompensa
                .FirstOrDefaultAsync(p => p.ProductoId == productoId);

            if (productoRecompensa == null || productoRecompensa.PuntosNecesarios != puntos)
            {
                return Json(new { success = false, message = "El producto seleccionado no existe o los puntos no coinciden." });
            }

            // Iniciar transacción
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // Restar puntos al usuario
                    usuarioPuntos.PuntosAcumulados -= puntos;
                    _context.UsuarioPuntos.Update(usuarioPuntos);

                    // Registrar el canje en el historial
                    var historialCanje = new HistorialCanje
                    {
                        UsuarioId = userId,
                        ProductoRecompensaId = productoRecompensa.Id,
                        PuntosCanjeados = puntos,
                        FechaCanje = DateTime.Now
                    };
                    _context.HistorialCanjes.Add(historialCanje);

                    // Aquí se puede añadir lógica para generar un pedido gratis con el producto canjeado
                    // Por ejemplo, crear un objeto Pedido y marcarlo como "Canje por Puntos"

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return Json(new { success = true });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return Json(new { success = false, message = "Error al procesar el canje: " + ex.Message });
                }
            }
        }

        // Método para actualizar los puntos del usuario después de una compra
        public static async Task ActualizarPuntosUsuario(ApplicationDbContext context, string userId, decimal montoCompra)
        {
            // Fórmula de cálculo de puntos: 100 puntos por cada dólar gastado
            int puntosGanados = (int)(montoCompra * 100);

            var usuarioPuntos = await context.UsuarioPuntos
                .FirstOrDefaultAsync(u => u.UsuarioId == userId);

            if (usuarioPuntos == null)
            {
                usuarioPuntos = new UsuarioPuntos
                {
                    UsuarioId = userId,
                    PuntosAcumulados = puntosGanados
                };
                context.UsuarioPuntos.Add(usuarioPuntos);
            }
            else
            {
                usuarioPuntos.PuntosAcumulados += puntosGanados;
                context.UsuarioPuntos.Update(usuarioPuntos);
            }

            await context.SaveChangesAsync();
        }
    }
}