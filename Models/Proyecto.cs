using System;
using System.Collections.Generic;

namespace ProyectoIdentity.Models;

public partial class Proyecto
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public List<Tarea>? Tareas { get; set; } // Relación con Tarea
}
