using System.ComponentModel.DataAnnotations;

namespace ProyectoIdentity.Models
{
	public class Conducta
	{
        public int ID { get; set; }

        public string TipoControl { get; set; }

        public int Efectividad { get; set; }
    }
}
