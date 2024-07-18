using System.ComponentModel.DataAnnotations;

namespace ProyectoIdentity.Models
{
    public class Asset
    {
        public int ID { get; set; }
        public string CodigoActivo { get; set; }

        public string Nombre { get; set; }

    }
}
