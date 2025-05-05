using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ProyectoIdentity.Models
{
    public class Tarea
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre de la tarea es obligatorio.")]
        [StringLength(21, ErrorMessage = "El nombre de la tarea no puede tener más de 100 caracteres.")]
        public string Nombredelatarea { get; set; }
        public DateTime FechadeInicio { get; set; }
        public DateTime FechadeFin { get; set; } // Nuevo atributo

        public double? TiempoEstimado { get; set; }
        public double? TiempoUtilizado { get; set; }
        public string EstadoProgreso { get; set; }
        public static List<SelectListItem> SumarTareasCompletadas()
        {
            return new List<SelectListItem>
            {
                new SelectListItem { Value = "1", Text = "Completada" },
                new SelectListItem { Value = "0", Text = "Pendiente" },
                new SelectListItem { Value = "0", Text = "En Progreso" }
            };
        }
        public int ProyectoId { get; set; }
        public int? EmpleadoId { get; set; }
        public double? Productividad { get; set; }
        public int Prioridad { get; set; }

        public static List<SelectListItem> GetPrioridades()
        {
            return new List<SelectListItem>
            {
                new SelectListItem { Value = "1", Text = "Baja" },
                new SelectListItem { Value = "2", Text = "Media" },
                new SelectListItem { Value = "3", Text = "Alta" }
            };
        }

        public Empleado Empleado { get; set; }
        public Proyecto Proyecto { get; set; }

       // public string Name { get; set; } // Nombre del empleado
        //public string LastName { get; set; } // Apellido del empleado
    }
}
