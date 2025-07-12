using ProyectoIdentity.Models;
using System;
using System.ComponentModel.DataAnnotations;

namespace ProyectoIdentity.Models
{
    public class CollectionPoint
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        public string Descripcion { get; set; }


        [Required]
        public string Address { get; set; }

        [Required]
        public double Latitude { get; set; }

        [Required]
        public double Longitude { get; set; }


        // Relación con sucursal
        public int SucursalId { get; set; }
        public Sucursal Sucursal { get; set; }


    }
}