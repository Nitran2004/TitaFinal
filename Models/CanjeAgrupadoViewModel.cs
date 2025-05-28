namespace ProyectoIdentity.Models
{
    // ViewModel para agrupar canjes del mismo momento
    public class CanjeAgrupadoViewModel
    {
        public DateTime FechaCanje { get; set; }
        public List<HistorialCanje> CanjesIndividuales { get; set; } = new List<HistorialCanje>();
        public int TotalPuntosUtilizados { get; set; }
        public int CantidadRecompensas { get; set; }
        public decimal ValorTotalAhorrado { get; set; }
        public string CodigoCanje { get; set; }

        // Propiedades calculadas
        public string CategoriasCanjeadas => string.Join(", ",
            CanjesIndividuales?.Select(c => c.ProductoRecompensa?.Categoria)
                              ?.Where(cat => !string.IsNullOrEmpty(cat))
                              ?.Distinct() ?? Enumerable.Empty<string>());
    }
}
