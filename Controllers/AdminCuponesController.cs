using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoIdentity.Datos;
using ProyectoIdentity.Models;
using System.Text.Json;

namespace ProyectoIdentity.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class AdminCuponesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminCuponesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Método auxiliar para generar código QR único
        private string GenerarCodigoQR()
        {
            string codigo;
            do
            {
                var random = new Random();
                var letras = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
                var numeros = "0123456789";

                codigo = "PROMO" +
                         new string(Enumerable.Repeat(numeros, 2).Select(s => s[random.Next(s.Length)]).ToArray()) +
                         "-" +
                         new string(Enumerable.Repeat(letras, 3).Select(s => s[random.Next(s.Length)]).ToArray()) +
                         new string(Enumerable.Repeat(numeros, 3).Select(s => s[random.Next(s.Length)]).ToArray());

            } while (_context.Cupones.Any(c => c.CodigoQR == codigo));

            return codigo;
        }

        // Vista principal de administración
        public async Task<IActionResult> Index()
        {
            var cupones = await _context.Cupones
                .OrderByDescending(c => c.Id)
                .ToListAsync();

            return View(cupones);
        }

        // Actualizar fecha de expiración via AJAX
        [HttpPost]
        public async Task<IActionResult> ActualizarFecha([FromBody] ActualizarFechaRequest request)
        {
            try
            {
                var cupon = await _context.Cupones.FindAsync(request.CuponId);
                if (cupon == null)
                {
                    return Json(new { success = false, message = "Cupón no encontrado" });
                }

                if (DateTime.TryParse(request.NuevaFecha, out DateTime fechaExpiracion))
                {
                    cupon.FechaExpiracion = fechaExpiracion;
                    await _context.SaveChangesAsync();

                    return Json(new
                    {
                        success = true,
                        message = "Fecha actualizada correctamente",
                        fechaFormateada = fechaExpiracion.ToString("dd/MM/yyyy")
                    });
                }
                else
                {
                    return Json(new { success = false, message = "Fecha no válida" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error interno: " + ex.Message });
            }
        }

        // Toggle estado activo/inactivo
        [HttpPost]
        public async Task<IActionResult> ToggleActivo([FromBody] ToggleActivoRequest request)
        {
            try
            {
                var cupon = await _context.Cupones.FindAsync(request.CuponId);
                if (cupon == null)
                {
                    return Json(new { success = false, message = "Cupón no encontrado" });
                }

                cupon.Activo = request.Activo;
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = $"Cupón {(request.Activo ? "activado" : "desactivado")} correctamente"
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error interno: " + ex.Message });
            }
        }
        // Editar cupón completo
        public async Task<IActionResult> Editar(int id)
        {
            var cupon = await _context.Cupones.FindAsync(id);
            if (cupon == null)
            {
                return NotFound();
            }

            return View(cupon);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(Cupon cupon)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(cupon);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Cupón actualizado correctamente";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    TempData["Error"] = "Error al actualizar: " + ex.Message;
                }
            }

            return View(cupon);
        }

        // Crear nuevo cupón
        public IActionResult Crear()
        {
            var cupon = new Cupon
            {
                Activo = true,
                FechaExpiracion = DateTime.Now.AddMonths(1),
                DiasAplicables = "Todos"
            };

            return View(cupon);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(Cupon cupon)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Generar código QR único
                    cupon.CodigoQR = GenerarCodigoQR();

                    _context.Add(cupon);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Cupón creado correctamente";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    TempData["Error"] = "Error al crear: " + ex.Message;
                }
            }

            return View(cupon);
        }

        // Eliminar cupón permanentemente
        [HttpPost]
        public async Task<IActionResult> Eliminar([FromBody] EliminarCuponRequest request)
        {
            try
            {
                var cupon = await _context.Cupones.FindAsync(request.CuponId);
                if (cupon == null)
                {
                    return Json(new { success = false, message = "Cupón no encontrado" });
                }

                // Verificar si el cupón tiene canjes asociados
                var tieneCanjes = await _context.CuponesCanjeados
                    .AnyAsync(cc => cc.CuponId == request.CuponId);

                if (tieneCanjes)
                {
                    // Eliminar primero los registros de canjes
                    var canjes = await _context.CuponesCanjeados
                        .Where(cc => cc.CuponId == request.CuponId)
                        .ToListAsync();

                    _context.CuponesCanjeados.RemoveRange(canjes);
                }

                // Eliminar el cupón
                _context.Cupones.Remove(cupon);
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = $"Cupón '{cupon.Nombre}' eliminado correctamente"
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Error al eliminar: " + ex.Message
                });
            }
        }
    }

    // Clases para requests AJAX
    public class ActualizarFechaRequest
    {
        public int CuponId { get; set; }
        public string NuevaFecha { get; set; }
    }

    public class ToggleActivoRequest
    {
        public int CuponId { get; set; }
        public bool Activo { get; set; }
    }

    public class EliminarCuponRequest
    {
        public int CuponId { get; set; }
    }

    // ViewModel para detalle
    public class DetalleCuponViewModel
    {
        public Cupon Cupon { get; set; }
        public int TotalCanjes { get; set; }
        public decimal TotalAhorrado { get; set; }
        public DateTime? UltimoUso { get; set; }
    }
}