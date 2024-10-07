using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoIdentity.Datos;
using ProyectoIdentity.Models;
using System.Linq;
using System.Threading.Tasks;

namespace ProyectoIdentity.Controllers
{
    [Authorize]
    public class MetasController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MetasController(ApplicationDbContext context)
        {
            _context = context;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index(string buscar)
        {
            IQueryable<Meta> metasQuery = _context.Metas;

            if (!string.IsNullOrEmpty(buscar))
            {
                metasQuery = metasQuery.Where(m => m.SiglaEG01_EG13.Contains(buscar));
            }

            var metas = await metasQuery.ToListAsync();
            return View(metas);
        }

        public IActionResult Crear()
        {
            return View();
        }

        [Authorize(Roles = "Administrador")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(Meta meta)
        {
            if (ModelState.IsValid)
            {
                _context.Add(meta);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(meta);
        }

        public IActionResult Editar(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var meta = _context.Metas.Find(id);
            if (meta == null)
            {
                return NotFound();
            }
            return View(meta);
        }

        [Authorize(Roles = "Administrador")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int id, Meta meta)
        {
            if (id != meta.ID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                _context.Update(meta);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(meta);
        }

        public IActionResult Borrar(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var meta = _context.Metas.Find(id);
            if (meta == null)
            {
                return NotFound();
            }

            return View(meta);
        }

        [Authorize(Roles = "Administrador")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BorrarConfirmed(int id)
        {
            var meta = await _context.Metas.FindAsync(id);
            if (meta == null)
            {
                return NotFound();
            }

            _context.Metas.Remove(meta);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Administrador")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BorrarTodo()
        {
            var todasLasMetas = await _context.Metas.ToListAsync();

            if (todasLasMetas.Any())
            {
                _context.Metas.RemoveRange(todasLasMetas);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
