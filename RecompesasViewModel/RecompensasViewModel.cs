using System;
using System.Collections.Generic;
using ProyectoIdentity.Models;  // Ajusta este namespace según tu proyecto

namespace ProyectoIdentity.ViewModels  // Ajusta este namespace según tu proyecto
{
    public class RecompensasViewModel
    {
        public int PuntosActuales { get; set; }
        public int PuntosMaximos { get; set; }
        public decimal PorcentajeProgreso { get; set; }
        public List<ProductoRecompensa> ProductosRecompensa { get; set; }
        public List<NivelRecompensa> NivelesRecompensa { get; set; }
        public List<string> Categorias { get; set; }
        public List<HistorialCanjeViewModel> HistorialCanjes { get; set; }

        public RecompensasViewModel()
        {
            ProductosRecompensa = new List<ProductoRecompensa>();
            NivelesRecompensa = new List<NivelRecompensa>();
            Categorias = new List<string>();
            HistorialCanjes = new List<HistorialCanjeViewModel>();
        }
    }

    public class NivelRecompensa
    {
        public string Nombre { get; set; }
        public int PuntosNecesarios { get; set; }
    }

    public class HistorialCanjeViewModel
    {
        public DateTime FechaCanje { get; set; }
        public string NombreProducto { get; set; }
        public int PuntosCanjeados { get; set; }
    }
}