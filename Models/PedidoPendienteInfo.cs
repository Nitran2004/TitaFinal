namespace ProyectoIdentity.Models
{
    public class PedidoPendienteInfo
    {
        public int Id { get; set; }
        public DateTime Fecha { get; set; }
        public decimal Total { get; set; }
        public string Estado { get; set; }
        public string TipoServicio { get; set; }

        public string FechaFormateada => Fecha.ToString("dd/MM/yyyy HH:mm");
        public string EstadoBadgeClass => Estado switch
        {
            "Preparándose" => "bg-warning",
            "Listo para entregar" => "bg-success",
            _ => "bg-secondary"
        };
    }
}
