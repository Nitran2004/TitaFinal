using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ProyectoIdentity.Datos;
using ProyectoIdentity.Models;

namespace ProyectoIdentity.Controllers
{
    [Authorize] // Aplica autorización globalmente para este controlador

    public class TareasController : Controller
    {
        private readonly ApplicationDbContext _context;
        public TareasController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Tareas
        public async Task<IActionResult> Index(DateTime? fechaInicio, DateTime? fechaFin)
        {
            if (fechaInicio > fechaFin)
            {
                ModelState.AddModelError("", "La fecha de inicio no puede ser mayor que la fecha de fin.");
                return View(await _context.Tareas.Include(t => t.Empleado).Include(t => t.Proyecto).ToListAsync());
            }

            IQueryable<Tarea> query = _context.Tareas.Include(t => t.Empleado).Include(t => t.Proyecto);

            if (fechaInicio.HasValue && fechaFin.HasValue)
            {
                query = query.Where(t => t.FechadeInicio >= fechaInicio && t.FechadeInicio <= fechaFin);
            }

            // Calcula la productividad de cada tarea
            var tareas = await query.ToListAsync();
            double Ptotal = 0;
            int tareasCompletadas = 0;

            foreach (var tarea in tareas)
            {
                if (tarea.EstadoProgreso == "Completada") // Solo considera tareas completadas
                {
                    double Pbase = tarea.Prioridad;
                    double Pfinal = Pbase;

                    // Aplica penalización o recompensa
                    if (tarea.TiempoUtilizado > tarea.TiempoEstimado)
                        Pfinal -= 0.5;
                    else if (tarea.TiempoUtilizado < tarea.TiempoEstimado)
                        Pfinal += 0.5;

                    tarea.Productividad = Pfinal; // Actualiza la propiedad de productividad
                    Ptotal += Pfinal;
                    tareasCompletadas++;
                }
            }

            // Calcula el promedio de productividad
            double Ppromedio = tareasCompletadas > 0 ? Ptotal / tareasCompletadas : 0;

            // Asigna los valores a ViewBag
            ViewBag.TotalTareas = tareas.Count;
            ViewBag.ProductividadPromedio = Ppromedio;

            return View(tareas);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CalcularProductividad()
        {
            var tareasCompletadas = await _context.Tareas
                .Where(t => t.EstadoProgreso == "Completada")
                .ToListAsync();

            if (tareasCompletadas == null || tareasCompletadas.Count == 0)
            {
                ViewBag.TotalTareasCompletadas = 0;
                ViewBag.ProductividadPromedio = 0;
                return View("CalcularProductividad", new List<Tarea>()); // Enviar lista vacía
            }

            double Ptotal = 0;
            foreach (var tarea in tareasCompletadas)
            {
                double Pbase = tarea.Prioridad;
                double Pfinal = Pbase;
                if (tarea.TiempoUtilizado > tarea.TiempoEstimado) Pfinal -= 0.5;
                else if (tarea.TiempoUtilizado < tarea.TiempoEstimado) Pfinal += 0.5;

                tarea.Productividad = Pfinal;
                Ptotal += Pfinal;
            }

            double Ppromedio = tareasCompletadas.Count > 0 ? Ptotal / tareasCompletadas.Count : 0;
            ViewBag.TotalTareasCompletadas = tareasCompletadas.Count;
            ViewBag.ProductividadPromedio = Ppromedio;

            // Obtener el primer empleado asignado a las tareas completadas (si hay alguna tarea completada)
            var empleadoId = tareasCompletadas.FirstOrDefault()?.EmpleadoId;
            if (empleadoId != null)
            {
                var empleado = await _context.Empleados
                    .FirstOrDefaultAsync(e => e.Id == empleadoId.Value);
                ViewBag.Empleado = empleado;
            }

            return View("CalcularProductividad", tareasCompletadas);
        }



        // GET: Tareas/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var tarea = await _context.Tareas
                .Include(t => t.Empleado)
                .Include(t => t.Proyecto)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (tarea == null)
            {
                return NotFound();
            }

            return View(tarea);
        }

        // GET: Tareas/Create
        [Authorize(Roles = "Administrador, Desarrollador")]
        public IActionResult Create()
        {
            PopulateDropdowns(); // Llenar los dropdowns para la vista
            return View();
        }



        // POST: Tareas/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Nombredelatarea,FechadeInicio,TiempoEstimado,TiempoUtilizado,EstadoProgreso,ProyectoId,EmpleadoId,Prioridad")] Tarea tarea)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Add(tarea);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error al guardar la tarea: {ex.Message}");
                }
            }

            PopulateDropdowns();
            return View(tarea);
        }




        // GET: Tareas/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var tarea = await _context.Tareas.FindAsync(id);
            if (tarea == null)
            {
                return NotFound();
            }

            PopulateDropdowns(tarea);
            return View(tarea);
        }

        [Authorize(Roles = "Administrador, Desarrollador")] // Solo los administradores pueden ver la vista de detalles

        // POST: Tareas/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Nombredelatarea,FechadeInicio,TiempoEstimado,TiempoUtilizado,EstadoProgreso,ProyectoId,EmpleadoId,Prioridad")] Tarea tarea)
        {
            if (id != tarea.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                // Verificar que el EmpleadoId y ProyectoId existen
                var empleadoExistente = await _context.Empleados.FindAsync(tarea.EmpleadoId);
                if (empleadoExistente == null)
                {
                    ModelState.AddModelError("EmpleadoId", "El empleado seleccionado no existe.");
                }

                var proyectoExistente = await _context.Proyectos.FindAsync(tarea.ProyectoId);
                if (proyectoExistente == null)
                {
                    ModelState.AddModelError("ProyectoId", "El proyecto seleccionado no existe.");
                }

                if (ModelState.IsValid) // Verifica nuevamente después de agregar errores
                {
                    try
                    {
                        _context.Update(tarea);
                        await _context.SaveChangesAsync();
                        return RedirectToAction(nameof(Index));
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        if (!TareaExists(tarea.Id))
                        {
                            return NotFound();
                        }
                        else
                        {
                            throw;
                        }
                    }
                }
            }

            PopulateDropdowns(tarea); // Llenar los dropdowns en caso de error
            return View(tarea);
        }

        // GET: Tareas/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var tarea = await _context.Tareas
                .Include(t => t.Empleado)
                .Include(t => t.Proyecto)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (tarea == null)
            {
                return NotFound();
            }

            return View(tarea);
        }

        [Authorize(Roles = "Administrador")] // Solo los administradores pueden ver la vista de detalles

        // POST: Tareas/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var tarea = await _context.Tareas.FindAsync(id);
            if (tarea != null)
            {
                _context.Tareas.Remove(tarea);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool TareaExists(int id)
        {
            return _context.Tareas.Any(e => e.Id == id);
        }

        // Método que suma las tareas completadas
        [HttpPost]
        public IActionResult SumarTareasCompletadas()
        {
            int totalCompletadas = _context.Tareas.Count(t => t.EstadoProgreso == "Completada");
            ViewBag.TotalCompletadas = totalCompletadas;
            return RedirectToAction(nameof(Index));
        }


        private void CargarEstados()
        {
            ViewBag.Estado = new SelectList(Tarea.SumarTareasCompletadas(), "Value", "Text");
        }

        private void PopulateDropdowns(Tarea tarea = null)
        {
            ViewBag.Prioridades = new SelectList(Tarea.GetPrioridades(), "Value", "Text", tarea?.Prioridad);
            ViewBag.Empleados = new SelectList(_context.Empleados, "Id", "Name", tarea?.EmpleadoId);
            ViewBag.Proyectos = new SelectList(_context.Proyectos, "Id", "Name", tarea?.ProyectoId);
            ViewBag.Estado = new SelectList(new List<SelectListItem>
        {
        new SelectListItem { Value = "En Progreso", Text = "En Progreso" },
        new SelectListItem { Value = "Completada", Text = "Completada" },
        new SelectListItem { Value = "Pendiente", Text = "Pendiente" }
        }, "Value", "Text", tarea?.EstadoProgreso);
        }

        // GET: Tareas/HistoricoProductividad/5
        [Authorize(Roles = "Administrador, Gerente")]
        public async Task<IActionResult> HistoricoProductividad(int empleadoId)
        {
            var empleado = await _context.Empleados.FindAsync(empleadoId);
            if (empleado == null)
            {
                return NotFound("El empleado especificado no existe.");
            }

            var tareasEmpleado = await _context.Tareas
                .Where(t => t.EmpleadoId == empleadoId)
                .OrderByDescending(t => t.FechadeInicio)
                .ToListAsync();

            // Calcular productividad para cada tarea
            foreach (var tarea in tareasEmpleado)
            {
                tarea.Productividad = tarea.TiempoUtilizado > 0
                    ? (tarea.TiempoEstimado / tarea.TiempoUtilizado) * 100 // Invertimos la fórmula aquí
                    : 0; // Evitar división por cero
            }

            // Calcular productividad total y promedio
            double productividadTotal = tareasEmpleado.Sum(t => t.Productividad ?? 0);
            double productividadPromedio = tareasEmpleado.Count > 0
                ? productividadTotal / tareasEmpleado.Count
                : 0;

            ViewBag.EmpleadoNombre = empleado.Name;
            ViewBag.ProductividadPromedio = productividadPromedio;
            ViewBag.TotalTareas = tareasEmpleado.Count;

            return View(tareasEmpleado);
        }

        public async Task<IActionResult> VerProductividadTareas()
        {
            // Obtener todas las tareas
            var tareas = await _context.Tareas
                                          .Include(t => t.Empleado) // Incluye el empleado relacionado con cada tarea
                                          .ToListAsync();

            // Recorrer cada tarea y calcular su productividad
            foreach (var tarea in tareas)
            {
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
                }
            }

            // Pasar las tareas a la vista para mostrar los resultados
            return View(tareas);
        }

        [Authorize(Roles = "Administrador, Gerente")]
        public async Task<IActionResult> ProductividadPorProyecto()
        {
            // Obtener proyectos únicos con sus tareas
            var proyectos = await _context.Proyectos
                .Include(p => p.Tareas)
                .ToListAsync();

            var proyectosProductividad = proyectos
                .Select(proyecto => {
                    // Filtrar solo tareas completadas para este proyecto
                    var tareasCompletadas = proyecto.Tareas
                        .Where(t => t.EstadoProgreso == "Completada")
                        .ToList();

                    // Calcular la productividad para el proyecto
                    int tareasCompletadasCount = tareasCompletadas.Count;
                    int totalTareasProyecto = proyecto.Tareas.Count;
                    double productividadPromedio = (double)tareasCompletadasCount / totalTareasProyecto * 100;

                    return new ProyectoProductividadViewModel
                    {
                        Proyecto = proyecto,
                        TotalTareas = totalTareasProyecto,
                        TareasCompletadas = tareasCompletadasCount,
                        ProductividadPromedio = Math.Round(productividadPromedio, 0) // Redondear a 0 decimales
                    };
                })
                .OrderByDescending(p => p.ProductividadPromedio)
                .ToList();

            return View(proyectosProductividad);
        }

        //public async Task<IActionResult> ReporteRetraso(DateTime? fechaInicio, DateTime? fechaFin)
        //{
        //    // Validar que las fechas sean válidas
        //    if (!fechaInicio.HasValue || !fechaFin.HasValue)
        //    {
        //        // Si no se proporcionan fechas, cargar todas las tareas retrasadas
        //        var todasTareasRetrasadas = await _context.Tareas
        //            .Where(t =>
        //                t.TiempoEstimado.HasValue &&
        //                t.TiempoUtilizado.HasValue &&
        //                t.TiempoUtilizado > t.TiempoEstimado)
        //            .Include(t => t.Empleado)
        //            .Select(t => new
        //            {
        //                t.Id,
        //                t.Nombredelatarea,
        //                t.FechadeInicio,
        //                t.FechadeFin,
        //                t.TiempoEstimado,
        //                t.TiempoUtilizado,
        //                DiasRetraso = t.FechadeFin > t.FechadeFin.AddHours(t.TiempoEstimado.Value)
        //                    ? Math.Ceiling(((decimal)(t.FechadeFin - t.FechadeFin.AddHours(t.TiempoEstimado.Value)).TotalHours) / 24m)
        //                    : 0,
        //                Empleado = t.Empleado != null ? $"{t.Empleado.Name} {t.Empleado.LastName}" : "Sin asignar"
        //            })
        //            .ToListAsync();

        //        return View(todasTareasRetrasadas);
        //    }

        //    // Validar que la fecha de inicio no sea mayor que la fecha de fin
        //    if (fechaInicio > fechaFin)
        //    {
        //        ModelState.AddModelError("", "La fecha de inicio no puede ser mayor que la fecha de fin.");
        //        return View(await _context.Tareas.Include(t => t.Empleado).ToListAsync());
        //    }

        //    // Filtrar tareas retrasadas dentro del rango de fechas
        //    var tareasRetrasadas = await _context.Tareas
        //        .Where(t =>
        //            // Filtrar por rango de fechas de fin
        //            t.FechadeFin >= fechaInicio && t.FechadeFin <= fechaFin &&

        //            // Verificar que tenga tiempo estimado y utilizado
        //            t.TiempoEstimado.HasValue && t.TiempoUtilizado.HasValue &&

        //            // Comparar si el tiempo utilizado supera el tiempo estimado
        //            t.TiempoUtilizado > t.TiempoEstimado)
        //        .Include(t => t.Empleado)
        //        .Select(t => new
        //        {
        //            t.Id,
        //            t.Nombredelatarea,
        //            t.FechadeInicio,
        //            t.FechadeFin,
        //            t.TiempoEstimado,
        //            t.TiempoUtilizado,

        //            // Calcular días de retraso (convirtiendo horas a días)
        //            DiasRetraso = t.FechadeFin > t.FechadeFin.AddHours(t.TiempoEstimado.Value)
        //                ? Math.Ceiling(((decimal)(t.FechadeFin - t.FechadeFin.AddHours(t.TiempoEstimado.Value)).TotalHours) / 24m)
        //                : 0,

        //            // Mostrar nombre del empleado o "Sin asignar"
        //            Empleado = t.Empleado != null ? $"{t.Empleado.Name} {t.Empleado.LastName}" : "Sin asignar"
        //        })
        //        .ToListAsync();

        //    // Pasar fechas de filtro a la vista
        //    ViewData["FechaInicio"] = fechaInicio;
        //    ViewData["FechaFin"] = fechaFin;

        //    return View(tareasRetrasadas);
        //}

        public async Task<IActionResult> ReporteRetraso(DateTime? fechaInicio, DateTime? fechaFin)
        {
            // Verifica si las fechas de inicio y fin están presentes
            if (!fechaInicio.HasValue || !fechaFin.HasValue)
            {
                ModelState.AddModelError("", "Debes especificar ambas fechas.");
                return View(new List<dynamic>()); // Devuelve una lista vacía si faltan fechas
            }

            // Verifica si la fecha de inicio es mayor que la fecha de fin
            if (fechaInicio > fechaFin)
            {
                ModelState.AddModelError("", "La fecha de inicio no puede ser mayor que la fecha de fin.");
                return View(new List<dynamic>()); // Devuelve una lista vacía si las fechas no son válidas
            }

            // Consulta las tareas retrasadas en la base de datos
            var tareasRetrasadas = await _context.Tareas
                .Where(t => t.FechadeInicio >= fechaInicio &&
                            t.FechadeInicio <= fechaFin &&
                            t.TiempoUtilizado > t.TiempoEstimado) // Filtra solo las tareas retrasadas
                .Select(t => new
                {
                    t.Nombredelatarea,
                    t.FechadeInicio,
                    t.TiempoEstimado,
                    t.TiempoUtilizado,
                    Empleado = t.Empleado != null ? $"{t.Empleado.Name} {t.Empleado.LastName}" : "Sin asignar"
                })
                .ToListAsync();

            // Retorna los datos dinámicos a la vista
            return View(tareasRetrasadas);
        }


    }
}
