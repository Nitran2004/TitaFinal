namespace ProyectoIdentity.Models
{
    public class Departamento
    {
        public int Id { get; set; } // Identificador único del departamento
        public string Nombre { get; set; } = null!; // Nombre del departamento

        // Relación con Gasto (un departamento puede tener varios gastos)
        public List<Gasto>? Gastos { get; set; }
    }
}
