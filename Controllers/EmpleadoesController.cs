using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ProyectoIdentity.Datos;
using ProyectoIdentity.Models;

namespace ProyectoIdentity.Controllers
{
    public class EmpleadoesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public EmpleadoesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Empleadoes
        public async Task<IActionResult> Index()
        {
            return View(await _context.Empleados.ToListAsync());
        }

        // GET: Empleadoes/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var empleado = await _context.Empleados
                .FirstOrDefaultAsync(m => m.Id == id);
            if (empleado == null)
            {
                return NotFound();
            }

            return View(empleado);
        }

        // GET: Empleadoes/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Empleadoes/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,LastName")] Empleado empleado)
        {
            if (ModelState.IsValid)
            {
                _context.Add(empleado);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(empleado);
        }

        // GET: Empleadoes/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var empleado = await _context.Empleados.FindAsync(id);
            if (empleado == null)
            {
                return NotFound();
            }
            return View(empleado);
        }

        // POST: Empleadoes/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,LastName")] Empleado empleado)
        {
            if (id != empleado.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(empleado);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!EmpleadoExists(empleado.Id))
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
            return View(empleado);
        }

        // GET: Empleadoes/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var empleado = await _context.Empleados
                .FirstOrDefaultAsync(m => m.Id == id);
            if (empleado == null)
            {
                return NotFound();
            }

            return View(empleado);
        }

        // POST: Empleadoes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var empleado = await _context.Empleados.FindAsync(id);
            if (empleado != null)
            {
                _context.Empleados.Remove(empleado);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool EmpleadoExists(int id)
        {
            return _context.Empleados.Any(e => e.Id == id);
        }

        public async Task<IActionResult> CalcularProductividadEmpleado(int empleadoId)
        {
            // Obtener el empleado junto con sus tareas
            var empleado = await _context.Empleados
                .Include(e => e.Tareas) // Asegurarnos de cargar las tareas
                .FirstOrDefaultAsync(e => e.Id == empleadoId);

            if (empleado == null || empleado.Tareas == null || empleado.Tareas.Count == 0)
            {
                ViewBag.ProductividadPromedio = "No hay tareas completadas";
                return RedirectToAction("Index"); // Regresar al índice si no hay tareas
            }

            double Ptotal = 0;
            int tareasCompletadas = 0;

            // Calcular la productividad
            foreach (var tarea in empleado.Tareas.Where(t => t.EstadoProgreso == "Completada"))
            {
                // Verificar que la tarea tenga datos válidos
                if (tarea.Prioridad > 0 && tarea.TiempoEstimado > 0 && tarea.TiempoUtilizado.HasValue)
                {
                    double Pbase = tarea.Prioridad;
                    double Pfinal = Pbase;

                    // Comparar tiempos y ajustar la productividad
                    if (tarea.TiempoUtilizado > tarea.TiempoEstimado)
                    {
                        Pfinal -= 0.5; // Penalización si el tiempo utilizado es mayor
                    }
                    else if (tarea.TiempoUtilizado < tarea.TiempoEstimado)
                    {
                        Pfinal += 0.5; // Bonificación si el tiempo utilizado es menor
                    }

                    // Asignar la productividad calculada
                    tarea.Productividad = Pfinal;

                    Ptotal += Pfinal; // Acumulamos la productividad total
                    tareasCompletadas++;
                }
            }

            // Calcular el promedio
            double Ppromedio = tareasCompletadas > 0 ? Ptotal / tareasCompletadas : 0;
            ViewBag.ProductividadPromedio = Ppromedio.ToString("0.00"); // Mostrar en formato de 2 decimales

            // Redirigir de nuevo al índice
            return RedirectToAction("Index");
        }




    }
}
