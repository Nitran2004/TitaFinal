// Agregar esta clase en tu carpeta Models o Models/ViewModels

using ProyectoIdentity.Models;

namespace ProyectoIdentity.Models
{
    public class ResumenCanjeViewModel
    {
        public HistorialCanje Canje { get; set; }
        public int PuntosRestantes { get; set; }
        public string CodigoCanje { get; set; }
        public AppUsuario Usuario { get; set; }

        // Propiedades calculadas para facilitar el uso en la vista
        public string NombreRecompensa => Canje?.ProductoRecompensa?.Nombre ?? "Recompensa";
        public decimal ValorOriginal => Canje?.ProductoRecompensa?.PrecioOriginal ?? 0m;
        public int PuntosUtilizados => Canje?.PuntosUtilizados ?? 0;
        public DateTime FechaCanje => Canje?.FechaCanje ?? DateTime.Now;
        public string CategoriaRecompensa => Canje?.ProductoRecompensa?.Categoria ?? "";

        // Para mostrar la imagen
        public byte[] ImagenRecompensa => Canje?.ProductoRecompensa?.Producto?.Imagen;
    }
}