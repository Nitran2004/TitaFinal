namespace ProyectoIdentity.Models
{
    public class CanjeAdminViewModel
    {
        public string UsuarioId { get; set; }
        public string NombreUsuario { get; set; }
        public string EmailUsuario { get; set; }
        public DateTime FechaCanje { get; set; }
        public string CodigoCanje { get; set; }
        public List<HistorialCanje> CanjesIndividuales { get; set; } = new List<HistorialCanje>();
        public int TotalPuntosUtilizados { get; set; }
        public int CantidadRecompensas { get; set; }
        public decimal ValorTotalAhorrado { get; set; }

        // Propiedades calculadas
        public string CategoriasCanjeadas => string.Join(", ",
            CanjesIndividuales?.Select(c => c.ProductoRecompensa?.Categoria)
                              ?.Where(cat => !string.IsNullOrEmpty(cat))
                              ?.Distinct() ?? Enumerable.Empty<string>());

        public string ResumenProductos => string.Join(", ",
            CanjesIndividuales?.Select(c => c.ProductoRecompensa?.Nombre)
                              ?.Where(nombre => !string.IsNullOrEmpty(nombre))
                              ?.Take(3) ?? Enumerable.Empty<string>()) +
            (CanjesIndividuales?.Count > 3 ? "..." : "");
    }
}
