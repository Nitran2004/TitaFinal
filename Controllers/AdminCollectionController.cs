using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoIdentity.Datos;
using ProyectoIdentity.Models;

namespace ProyectoIdentity.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class AdminCollectionController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminCollectionController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var points = await _context.CollectionPoints
                .Include(cp => cp.Sucursal)
                .ToListAsync();
            return View(points);
        }

        public async Task<IActionResult> Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(CollectionPoint model, string Latitude, string Longitude)
        {
            try
            {
                // Convertir coordenadas usando formato invariante (punto como decimal)
                if (!string.IsNullOrEmpty(Latitude))
                {
                    model.Latitude = Convert.ToDouble(Latitude, System.Globalization.CultureInfo.InvariantCulture);
                }
                if (!string.IsNullOrEmpty(Longitude))
                {
                    model.Longitude = Convert.ToDouble(Longitude, System.Globalization.CultureInfo.InvariantCulture);
                }

                // Usar la primera sucursal que exista
                var primeraSucursal = await _context.Sucursales.FirstOrDefaultAsync();
                if (primeraSucursal != null)
                {
                    model.SucursalId = primeraSucursal.Id;
                }
                else
                {
                    TempData["Error"] = "No hay sucursales disponibles. Crea una sucursal primero.";
                    return View(model);
                }

                _context.CollectionPoints.Add(model);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Punto creado exitosamente";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                var innerError = ex.InnerException?.Message ?? "Sin error interno";
                var fullError = $"{ex.Message} | Inner: {innerError}";

                TempData["Error"] = $"Error: {fullError}";
                return View(model);
            }
        }

        public async Task<IActionResult> Edit(int id)
        {
            var point = await _context.CollectionPoints.FindAsync(id);
            if (point == null) return NotFound();

            ViewBag.Sucursales = await _context.Sucursales.ToListAsync();
            return View(point);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(CollectionPoint model, string Latitude, string Longitude)
        {
            try
            {
                // Convertir coordenadas usando formato invariante
                if (!string.IsNullOrEmpty(Latitude))
                {
                    model.Latitude = Convert.ToDouble(Latitude, System.Globalization.CultureInfo.InvariantCulture);
                }
                if (!string.IsNullOrEmpty(Longitude))
                {
                    model.Longitude = Convert.ToDouble(Longitude, System.Globalization.CultureInfo.InvariantCulture);
                }

                _context.Update(model);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Punto actualizado exitosamente";
                return RedirectToAction("Index");
            }
            catch
            {
                TempData["Error"] = "Error al actualizar el punto";
                ViewBag.Sucursales = await _context.Sucursales.ToListAsync();
                return View(model);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Delete([FromBody] int id)
        {
            try
            {
                Console.WriteLine($"[DEBUG] Intentando eliminar punto con ID: {id}");

                var point = await _context.CollectionPoints.FindAsync(id);
                if (point != null)
                {
                    Console.WriteLine($"[DEBUG] Punto encontrado: {point.Name}");
                    _context.CollectionPoints.Remove(point);
                    await _context.SaveChangesAsync();
                    return Json(new { success = true, message = "Punto eliminado exitosamente" });
                }

                Console.WriteLine($"[DEBUG] Punto con ID {id} no encontrado");
                return Json(new { success = false, message = "Punto no encontrado" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Error al eliminar: {ex.Message}");
                return Json(new { success = false, message = "Error al eliminar: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ToggleActive(int id)
        {
            try
            {
                var point = await _context.CollectionPoints.FindAsync(id);
                if (point != null)
                {
                    // Si no tiene propiedad IsActive, crear una lógica alternativa
                    // Por ejemplo, cambiar el nombre para indicar estado
                    var isCurrentlyActive = !point.Name.Contains("[INACTIVO]");

                    if (isCurrentlyActive)
                    {
                        point.Name = point.Name + " [INACTIVO]";
                    }
                    else
                    {
                        point.Name = point.Name.Replace(" [INACTIVO]", "");
                    }

                    await _context.SaveChangesAsync();
                    return Json(new
                    {
                        success = true,
                        isActive = !isCurrentlyActive,
                        message = isCurrentlyActive ? "Punto desactivado" : "Punto activado"
                    });
                }
                return Json(new { success = false, message = "Punto no encontrado" });
            }
            catch
            {
                return Json(new { success = false, message = "Error al cambiar estado" });
            }
        }
    }
}