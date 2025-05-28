// Agregar esta clase en tu carpeta Models o Models/ViewModels

using ProyectoIdentity.Models;

namespace ProyectoIdentity.Models
{
    public class HistorialCompletoViewModel
    {
        public AppUsuario Usuario { get; set; }
        public int PuntosActuales { get; set; }
        public List<TransaccionPuntos> TransaccionesPuntos { get; set; } = new List<TransaccionPuntos>();
        public List<Pedido> Pedidos { get; set; } = new List<Pedido>();
        public List<HistorialCanje> CanjesRecompensas { get; set; } = new List<HistorialCanje>();

        // ✅ NUEVA PROPIEDAD: Para mostrar canjes agrupados con códigos
        public List<HistorialCanjeConCodigoViewModel> CanjesConCodigos { get; set; } = new List<HistorialCanjeConCodigoViewModel>();

        // Propiedades calculadas para estadísticas
        public int TotalPedidosRealizados => Pedidos?.Count ?? 0;
        public decimal TotalGastado => Pedidos?.Sum(p => p.Total) ?? 0;
        public int TotalPuntosGanados => TransaccionesPuntos?.Where(t => t.Puntos > 0).Sum(t => t.Puntos) ?? 0;
        public int TotalRecompensasCanjeadas => CanjesRecompensas?.Count ?? 0;
    }
}