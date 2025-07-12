using System;

namespace ProyectoIdentity.Services
{
    public static class DistanceCalculator
    {
        /// <summary>
        /// Calcula la distancia entre dos puntos geográficos usando la fórmula de Haversine
        /// </summary>
        /// <param name="lat1">Latitud del primer punto</param>
        /// <param name="lon1">Longitud del primer punto</param>
        /// <param name="lat2">Latitud del segundo punto</param>
        /// <param name="lon2">Longitud del segundo punto</param>
        /// <returns>Distancia en kilómetros</returns>
        public static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371; // Radio de la Tierra en kilómetros

            // Convertir grados a radianes
            double dLat = ToRadians(lat2 - lat1);
            double dLon = ToRadians(lon2 - lon1);

            // Fórmula de Haversine
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return R * c;
        }

        private static double ToRadians(double degrees)
        {
            return degrees * Math.PI / 180;
        }
    }
}