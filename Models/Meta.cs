using System.ComponentModel.DataAnnotations;

namespace ProyectoIdentity.Models
{
    public class Meta
    {
        public int ID { get; set; }

        [Display(Name = "Codigo")]
        public string Codigo { get; set; }

        [Display(Name = "Dominio")]
        public string Dominio { get; set; }

        [Display(Name = "Nivel de Importancia")]
        public string Nivel { get; set; }

        [Display(Name = "Alineamiento y objetivos de gobierno Sigla EG01-EG13")]
        public string SiglaEG01_EG13 { get; set; } // Renombrar para evitar caracteres especiales
    }
}
