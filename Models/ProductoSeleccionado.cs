namespace ProyectoIdentity.Models
{
    public class ProductoSeleccionado
    {
        public int ProductoId { get; set; }
        //public string Nombre { get; set; } = "";

        public string Nombre { get; set; } = string.Empty;

        public decimal Precio { get; set; }
        public int Cantidad { get; set; }
        public bool Seleccionado { get; set; }
        public decimal Subtotal { get; set; }


        public bool AplicaDescuento { get; set; }

    }
}
