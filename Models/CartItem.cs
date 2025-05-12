using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ProyectoIdentity.Models;

namespace ProyectoIdentity.Models
{
    /// <summary>
    /// Clase que representa un item del carrito para deserializar el JSON recibido del cliente.
    /// Esta NO es una entidad de base de datos, solo un DTO para transferir datos.
    /// </summary>
    public class CarritoItem
    {
        public string Id { get; set; }
        public string Nombre { get; set; }
        public decimal Precio { get; set; }
        public int Cantidad { get; set; }
    }
}