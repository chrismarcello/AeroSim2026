using System;
using AeroSim2026.Models;

namespace AeroSim2026.Core.Converters
{
    public static class CoordinatesDistance
    {
        public static double DistanceTo(this Coordinates baseCoordinates, Coordinates targetCoordinates, UnitOfLength unitOfLength)
        {
            var R = 6371; // Earth's radius in kilometers

            var lat1Rad = ToRadians(baseCoordinates.Latitude);
            var lon1Rad = ToRadians(baseCoordinates.Longitude);
            var lat2Rad = ToRadians(targetCoordinates.Latitude);
            var lon2Rad = ToRadians(targetCoordinates.Longitude);

            var dLat = lat2Rad - lat1Rad;
            var dLon = lon2Rad - lon1Rad;

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(lat1Rad) * Math.Cos(lat2Rad) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            var distance = R * c; // Distance in kilometers

            return unitOfLength.ConvertFromKilometers(distance);
        }

        private static double ToRadians(double degrees)
        {
            return degrees * Math.PI / 180;
        }
    }
}
