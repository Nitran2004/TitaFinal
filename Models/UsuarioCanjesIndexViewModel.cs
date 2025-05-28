namespace ProyectoIdentity.Models
{
    public class UsuarioCanjesIndexViewModel
    {
        public string UsuarioId { get; set; }
        public string UserName { get; set; }
        public string Nombre { get; set; }
        public string Email { get; set; }
        public int PuntosFidelidad { get; set; }
        public int TotalCanjes { get; set; }
        public DateTime UltimoCanje { get; set; }
        public int TotalPuntosUtilizados { get; set; }
    }
}
