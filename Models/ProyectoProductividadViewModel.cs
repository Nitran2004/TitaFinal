namespace ProyectoIdentity.Models
{
    public class ProyectoProductividadViewModel
    {
        public Proyecto Proyecto { get; set; }
        public int TotalTareas { get; set; }
        public int TareasCompletadas { get; set; }
        public double ProductividadPromedio { get; set; }
    }
}