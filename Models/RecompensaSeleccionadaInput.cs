namespace ProyectoIdentity.Models
{
    public class RecompensaSeleccionadaInput
    {
        public int RecompensaId { get; set; }
        public bool Seleccionada { get; set; }
        public int Cantidad { get; set; } = 1;
    }
}
