using System;
using System.Collections.Generic;
using System.Text;

namespace AeroSim2026.Core.Routing
{
    public static class GeoMath
    {
        private const double RadiusNM = 3440.1;

        public static double Distance(double lat1, double lon1, double lat2, double lon2)
        {
            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return RadiusNM * c;
        }
        public static double ToRadians(double angle)
        {
            return angle * Math.PI / 180.0;
        }
        public static double Bearing(double lat1, double lon1, double lat2, double lon2)
        {
            var dLon = (lon2 - lon1) * Math.PI / 180.0;
            var lat1Rad = lat1 * Math.PI / 180.0;
            var lat2Rad = lat2 * Math.PI / 180.0;

            var y = Math.Sin(dLon) * Math.Cos(lat2Rad);
            var x = Math.Cos(lat1Rad) * Math.Sin(lat2Rad) -
                    Math.Sin(lat1Rad) * Math.Cos(lat2Rad) * Math.Cos(dLon);

            var brng = Math.Atan2(y, x) * 180.0 / Math.PI;
            return (brng + 360) % 360; // Normalize to 0-360
        }

        /// <summary>
        /// Generates intermediate points along a Great Circle route between two coordinates.
        /// Returns a list of raw (Latitude, Longitude) tuples.
        /// </summary>
        public static List<(double Latitude, double Longitude)> GenerateGreatCirclePoints(
            double lat1, double lon1, double lat2, double lon2, int numPoints = 100)
        {
            var points = new List<(double Latitude, double Longitude)>();

            // 1. Convert to radians using your existing helper!
            double rLat1 = ToRadians(lat1);
            double rLon1 = ToRadians(lon1);
            double rLat2 = ToRadians(lat2);
            double rLon2 = ToRadians(lon2);

            // 2. Calculate the angular distance
            double d = 2 * Math.Asin(Math.Sqrt(
                Math.Pow(Math.Sin((rLat2 - rLat1) / 2), 2) +
                Math.Cos(rLat1) * Math.Cos(rLat2) * Math.Pow(Math.Sin((rLon2 - rLon1) / 2), 2)));

            // Safety check: If points are identical, avoid division by zero (NaN)
            if (d == 0)
            {
                points.Add((lat1, lon1));
                return points;
            }

            // 3. Interpolate the points
            for (int i = 0; i <= numPoints; i++)
            {
                double f = (double)i / numPoints;
                double A = Math.Sin((1 - f) * d) / Math.Sin(d);
                double B = Math.Sin(f * d) / Math.Sin(d);

                double x = A * Math.Cos(rLat1) * Math.Cos(rLon1) + B * Math.Cos(rLat2) * Math.Cos(rLon2);
                double y = A * Math.Cos(rLat1) * Math.Sin(rLon1) + B * Math.Cos(rLat2) * Math.Sin(rLon2);
                double z = A * Math.Sin(rLat1) + B * Math.Sin(rLat2);

                // Convert back to degrees
                double finalLat = Math.Atan2(z, Math.Sqrt(x * x + y * y)) * (180.0 / Math.PI);
                double finalLon = Math.Atan2(y, x) * (180.0 / Math.PI);

                points.Add((finalLat, finalLon));
            }

            return points;
        }
    }
}
