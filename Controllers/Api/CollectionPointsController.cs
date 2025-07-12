using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoIdentity.Datos;
using ProyectoIdentity.Models;
using ProyectoIdentity.Services; // Añadido para DistanceCalculator
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ProyectoIdentity.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CollectionPointsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CollectionPointsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Clase de respuesta para devolver información de distancia
        public class CollectionPointDistanceResponse
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
        public async Task<ActionResult<IEnumerable<CollectionPointDistanceResponse>>> GetNearestCollectionPoints(
            [FromQuery] double latitude,
            [FromQuery] double longitude,
            [FromQuery] double maxDistance = 50,
            [FromQuery] int limit = 5)
        {
            // Logs detallados
            Console.WriteLine($"Coordenadas recibidas - Lat: {latitude}, Lon: {longitude}");

            var collectionPoints = await _context.CollectionPoints.ToListAsync();

            var nearestPoints = collectionPoints
                .Select(point => {
                    // Cálculo de distancia con log detallado
                    var distance = DistanceCalculator.CalculateDistance(
                        latitude, longitude,
                        point.Latitude, point.Longitude);

                    Console.WriteLine($"Punto: {point.Name}");
                    Console.WriteLine($"Punto Lat: {point.Latitude}, Lon: {point.Longitude}");
                    Console.WriteLine($"Distancia calculada: {distance} km");

                    return new CollectionPointDistanceResponse
                    {
                        CollectionPointId = point.Id,
                        Name = point.Name,
                        Address = point.Address,
                        Latitude = point.Latitude,
                        Longitude = point.Longitude,
                        DistanceInKm = distance
                    };
                })
                .Where(p => p.DistanceInKm <= maxDistance)
                .OrderBy(p => p.DistanceInKm)
                .Take(limit)
                .ToList();

            // Log adicional
            Console.WriteLine($"Puntos encontrados: {nearestPoints.Count}");

            return Ok(nearestPoints);
        }

        /// <summary>
        /// Obtiene todos los puntos de recolección
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CollectionPoint>>> GetCollectionPoints()
        {
            var points = await _context.CollectionPoints.ToListAsync();

            if (!points.Any())
            {
                return NotFound("No hay puntos de recolección disponibles");
            }

            return Ok(points);
        }


    }
}