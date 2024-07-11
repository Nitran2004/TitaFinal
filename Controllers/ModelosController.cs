using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ProyectoIdentity.Datos;
using ProyectoIdentity.Models;
using System.Linq;
using System.Threading.Tasks;

namespace ProyectoIdentity.Controllers
{
    [Authorize] // Aplica autorización globalmente para este controlador
    public class ModelosController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ModelosController(ApplicationDbContext context)
        {
            _context = context;
        }

        [AllowAnonymous] // Permitir acceso público a la lista de modelos
        public async Task<IActionResult> Index()
        {
            var modelos = await _context.Modelos.Include(m => m.Alineamiento).ToListAsync();
            return View(modelos);
        }

        [Authorize(Roles = "Administrador")] // Solo los administradores pueden ver la vista de detalles
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var modelo = await _context.Modelos
                .Include(m => m.Alineamiento)
                .FirstOrDefaultAsync(m => m.ModeloID == id);
            if (modelo == null)
            {
                return NotFound();
            }

            return View(modelo);
        }

        //[Authorize(Roles = "Administrador")] // Solo los administradores pueden crear
        public IActionResult Create()
        {
            //ViewData["AlineamientoID"] = new SelectList(_context.Alineamientos, "ID", "Codigo");
            return View();
        }

        [Authorize(Roles = "Administrador")] // Solo los administradores pueden crear
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Core,Alineamiento2")] Modelo modelo)
        {
            if (ModelState.IsValid)
            {
                _context.Add(modelo);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["AlineamientoID"] = new SelectList(_context.Alineamientos, "ID", "Codigo", modelo.AlineamientoID);
            return View(modelo);
        }

        //[Authorize(Roles = "Administrador")] // Solo los administradores pueden editar
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var modelo = await _context.Modelos.FindAsync(id);
            if (modelo == null)
            {
                return NotFound();
            }
            ViewData["AlineamientoID"] = new SelectList(_context.Alineamientos, "ID", "Codigo", modelo.AlineamientoID);
            return View(modelo);
        }

        [Authorize(Roles = "Administrador")] // Solo los administradores pueden editar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ModeloID,Core,AlineamientoID")] Modelo modelo)
        {
            if (id != modelo.ModeloID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(modelo);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ModeloExists(modelo.ModeloID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["AlineamientoID"] = new SelectList(_context.Alineamientos, "ID", "Codigo", modelo.AlineamientoID);
            return View(modelo);
        }

        // GET: Modelo/Delete/5
        //[Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Modelos == null)
            {
                return NotFound();
            }

            var modelo = await _context.Modelos
                .Include(m => m.Alineamiento)
                .FirstOrDefaultAsync(m => m.ModeloID == id);
            if (modelo == null)
            {
                return NotFound();
            }

            return View(modelo);
        }

        // POST: Modelo/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var modelo = await _context.Modelos.FindAsync(id);
            if (modelo == null)
            {
                return NotFound();
            }

            _context.Modelos.Remove(modelo);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }




        private bool ModeloExists(int id)
        {
            return _context.Modelos.Any(e => e.ModeloID == id);
        }
    }
}
