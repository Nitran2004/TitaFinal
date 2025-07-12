namespace ProyectoIdentity.Models
{
    // Modelo
    public class Sucursal
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Direccion { get; set; }

        public double Latitud { get; set; }
        public double Longitud { get; set; }
    }

}
