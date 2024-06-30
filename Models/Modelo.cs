using System.ComponentModel.DataAnnotations;

namespace ProyectoIdentity.Models
{
    public class Modelo
    {
        public int ModeloID { get; set; }

        [Display(Name = "ModeloCore")]
        public string Core { get; set; }

        public int AlineamientoID { get; set; }
        public Alineamiento? Alineamiento { get; set; }
    }
}
