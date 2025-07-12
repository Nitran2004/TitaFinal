namespace ProyectoIdentity.Models
{
    public class LimiteAlcanzadoViewModel
    {
        public int PedidosActivos { get; set; }
        public int LimiteMaximo { get; set; }
        public List<PedidoPendienteInfo> PedidosPendientes { get; set; } = new();
        public string MensajePersonalizado =>
            $"Tienes {PedidosActivos} de {LimiteMaximo} pedidos activos. Espera a que se entreguen para hacer más pedidos.";
    }
}
