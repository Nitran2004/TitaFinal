using System.Collections.Generic;

namespace ProyectoIdentity.Models
{
    public class Empleado
    {
        public int Id { get; set; } // Cambia el nombre de la propiedad a EmpleadoId para seguir la misma convención

        public string Name { get; set; } = null!;
        public string LastName { get; set; } = null!;

        public List<Tarea>? Tareas { get; set; } // Relación con Tarea
        public List<Gasto>? Gastos { get; set; }
    }
}
