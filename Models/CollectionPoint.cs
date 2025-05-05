using Microsoft.EntityFrameworkCore;

namespace ProyectoIdentity.Models
{
    public class CollectionPoint
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }


}
