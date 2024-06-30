using System.ComponentModel.DataAnnotations;

namespace ProyectoIdentity.Models
{
    public class Alineamiento
    {
        public int ID { get; set; }

        [Display(Name = "Codigo")]
        public string Codigo { get; set; }

        [Display(Name = "Dominio")]
        public string Dominio { get; set; }

        [Display(Name = "Nivel de Importancia")]
        public string Nivel { get; set; }

        [Display(Name = "Alineamiento y objetivos de gobierno Sigla AG01-AG13")]
        public string SiglaAG01_AG13 { get; set; }

        public List<Modelo>? Modelos { get; set; } // Agrega el using para el espacio de nombres donde se define Comentario


    }
}
