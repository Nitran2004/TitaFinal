namespace ProyectoIdentity.Models
{
    public class Gasto
    {
        public int ID { get; set; } // Identificador único del gasto
        public DateTime Fecha { get; set; } // Fecha en la que se realizó el gasto
        public string Descripcion { get; set; } // Breve descripción del gasto
        public decimal Monto { get; set; } // Monto asociado al gasto

        // Relación con Empleado
        public int IdEmpleado { get; set; } // Clave foránea
        public Empleado Empleado { get; set; } = null!; // Navegabilidad

        // Relación con Departamento
        public int IdDepartamento { get; set; } // Clave foránea
        public Departamento Departamento { get; set; } = null!; // Navegabilidad
    }
}
