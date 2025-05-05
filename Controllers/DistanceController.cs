using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoIdentity.Datos;
using ProyectoIdentity.Servicios;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ProyectoIdentity.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DistanceController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public DistanceController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Clase de respuesta para devolver información de distancia
        public class DistanceResponse
        {
            public int CollectionPointId { get; set; }
            public string Name { get; set; }
            public string Address { get; set; }
            public double Latitude { get; set; }
            public double Longitude { get; set; }
            public double DistanceInKm { get; set; }
        }

        /// <summary>
        /// Obtiene los puntos de recolección más cercanos a las coordenadas proporcionadas
        /// </summary>
        /// <param name="latitude">Latitud del punto de origen</param>
        /// <param name="longitude">Longitud del punto de origen</param>
        /// <param name="maxDistance">Distancia máxima en km (opcional, por defecto 50)</param>
        /// <param name="limit">Número máximo de puntos a devolver (opcional, por defecto 5)</param>
        [HttpGet("nearest")]
        public async Task<ActionResult<IEnumerable<DistanceResponse>>> GetNearestCollectionPoints(
            [FromQuery] double latitude,
            [FromQuery] double longitude,
            [FromQuery] double maxDistance = 50,
            [FromQuery] int limit = 5)
        {
            // Obtener todos los puntos de recolección
            var collectionPoints = await _context.CollectionPoints.ToListAsync();

            // Calcular distancias y filtrar
            var nearestPoints = collectionPoints
                .Select(point => new DistanceResponse
                {
                    CollectionPointId = point.Id,
                    Name = point.Name,
                    Address = point.Address,
                    Latitude = point.Latitude,
                    Longitude = point.Longitude,
                    DistanceInKm = DistanceCalculator.CalculateDistance(
                        latitude, longitude,
                        point.Latitude, point.Longitude)
                })
                .Where(p => p.DistanceInKm <= maxDistance)
                .OrderBy(p => p.DistanceInKm)
                .Take(limit)
                .ToList();

            return Ok(nearestPoints);
        }

        /// <summary>
        /// Calcula la distancia a un punto de recolección específico
        /// </summary>
        [HttpGet("distance/{collectionPointId}")]
        public async Task<ActionResult<DistanceResponse>> GetDistanceToCollectionPoint(
            int collectionPointId,
            [FromQuery] double latitude,
            [FromQuery] double longitude)
        {
            // Buscar el punto de recolección
            var point = await _context.CollectionPoints.FindAsync(collectionPointId);

            if (point == null)
            {
                return NotFound("Punto de recolección no encontrado");
            }

            // Calcular distancia
            var distance = DistanceCalculator.CalculateDistance(
                latitude, longitude,
                point.Latitude, point.Longitude);

            var response = new DistanceResponse
            {
                CollectionPointId = point.Id,
                Name = point.Name,
                Address = point.Address,
                Latitude = point.Latitude,
                Longitude = point.Longitude,
                DistanceInKm = distance
            };

            return Ok(response);
        }
    }
}