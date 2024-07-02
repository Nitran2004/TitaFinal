using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoIdentity.Datos;
using ProyectoIdentity.Models;
using System.Linq;
using System.Threading.Tasks;

namespace ProyectoIdentity.Controllers
{
    [Authorize] // Aplica autorización globalmente para este controlador
    public class AlineamientosController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AlineamientosController(ApplicationDbContext context)
        {
            _context = context;
        }

        [AllowAnonymous] // Permitir acceso público a la lista de alineamientos
        public async Task<IActionResult> Index(string buscar)
        {
            // Filtrar por SiglaAG01_AG13 si hay término de búsqueda
            IQueryable<Alineamiento> alineamientosQuery = _context.Alineamientos;

            if (!string.IsNullOrEmpty(buscar))
            {
                alineamientosQuery = alineamientosQuery.Where(m => m.SiglaAG01_AG13.Contains(buscar));
            }

            // Obtener la lista de alineamientos filtrados
            var alineamientos = await alineamientosQuery.ToListAsync();

            return View(alineamientos);
        }

        [Authorize(Roles = "Administrador")] // Solo los administradores pueden ver la vista de detalles
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var alineamiento = await _context.Alineamientos
                .FirstOrDefaultAsync(m => m.ID == id);
            if (alineamiento == null)
            {
                return NotFound();
            }

            return View(alineamiento);
        }

        //[Authorize(Roles = "Administrador")] // Solo los administradores pueden crear
        public IActionResult Create()
        {
            return View();
        }

        [Authorize(Roles = "Administrador")] // Solo los administradores pueden crear
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ID,Codigo,Dominio,Nivel,SiglaAG01_AG13")] Alineamiento alineamiento)
        {
            if (ModelState.IsValid)
            {
                _context.Add(alineamiento);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(alineamiento);
        }

        //[Authorize(Roles = "Administrador")] // Solo los administradores pueden editar
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var alineamiento = await _context.Alineamientos.FindAsync(id);
            if (alineamiento == null)
            {
                return NotFound();
            }
            return View(alineamiento);
        }

        [Authorize(Roles = "Administrador")] // Solo los administradores pueden editar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ID,Codigo,Dominio,Nivel,SiglaAG01_AG13")] Alineamiento alineamiento)
        {
            if (id != alineamiento.ID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(alineamiento);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AlineamientoExists(alineamiento.ID))
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
            return View(alineamiento);
        }

        // GET: Alineamiento/Delete/5
        //[Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var alineamiento = await _context.Alineamientos
                .FirstOrDefaultAsync(m => m.ID == id);
            if (alineamiento == null)
            {
                return NotFound();
            }

            return View(alineamiento);
        }

        // POST: Alineamiento/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var alineamiento = await _context.Alineamientos.FindAsync(id);
            if (alineamiento == null)
            {
                return NotFound();
            }

            _context.Alineamientos.Remove(alineamiento);
            await _context.SaveChangesAsync();

            return Ok(); // Devuelve una respuesta 200 OK si la eliminación es exitosa
        }



        private bool AlineamientoExists(int id)
        {
            return _context.Alineamientos.Any(e => e.ID == id);
        }
    }
}
