using ProyectoIdentity.Models;

namespace ProyectoIdentity.Models
{
    public class Pedido
    {
        public int Id { get; set; }
        public string? UsuarioId { get; set; } // Puede ser null si no se ha registrado
        public ICollection<PedidoProducto> PedidoProductos { get; set; }

        public DateTime Fecha { get; set; }
        public decimal Total { get; set; }

        public List<PedidoDetalle> Detalles { get; set; }

        public string Estado { get; set; } = "Preparándose"; // ← NUEVO: valor inicial por defecto

        // Relación con productos
        public List<Producto> Productos { get; set; } = new List<Producto>();

        // Relación con sucursal
        public int SucursalId { get; set; }
        public Sucursal Sucursal { get; set; }

        // Nuevos campos para comentarios y calificación
        public string? Comentario { get; set; }
        public int? Calificacion { get; set; } // 1-5 estrellas
        public bool ComentarioEnviado { get; set; } = false;

        // Añadir referencia al punto de recolección
        public int? PuntoRecoleccionId { get; set; }
        public CollectionPoint PuntoRecoleccion { get; set; }

        public bool EsCupon { get; set; } = false;

    }


}
