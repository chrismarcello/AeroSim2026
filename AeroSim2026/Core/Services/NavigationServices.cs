using AeroSim2026.EFModels;
using AeroSim2026.Core.Routing; // Required for RoutingGraph
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AeroSim2026.Core.Services
{
    public class NavigationServices : INavigationServices
    {
        private readonly ILogger<NavigationServices> _logger;
        private readonly Aerosim2026Context _context;
        private const double EarthRadiusNm = 3440.1;

        public NavigationServices(ILogger<NavigationServices> logger, Aerosim2026Context context)
        {
            _logger = logger;
            _context = context;
        }

        private static double ToRadians(double degrees) => degrees * (Math.PI / 180);
        private static double ToDegrees(double radians) => radians * (180 / Math.PI);

        // ====================================================================
        // GRAPH INITIALIZATION (The Bridge to your new A* Engine)
        // ====================================================================
        public async Task InitializeRoutingGraphAsync(RoutingGraph routingGraph)
        {
            try
            {
                _logger.LogInformation("Loading routing data from database...");

                // Fetch Airways WITH their related Waypoint identifiers
                var airways = await _context.Airways
                    .AsNoTracking()
                    .Include(a => a.FromWaypoint)
                    .Include(a => a.ToWaypoint)
                    .ToListAsync();

                var navSearches = await _context.NavSearches
                    .AsNoTracking()
                    //.Where(ns => ns.WaypointId != null)
                    .Select(ns => new
                    {
                        WaypointId = ns.WaypointId!.Value,
                        // This is the exact null-checking logic you remembered!
                        NavType = ns.VorId != null ? "VOR" :
                                  ns.NdbId != null ? "NDB" :
                                  "WAYPOINT"
                    })
                    .ToListAsync();

                var navTypeLookup = navSearches
                    .GroupBy(ns => ns.WaypointId)
                    .ToDictionary(g => g.Key, g => g.First().NavType);
                //Console.WriteLine("Just testing");
                // Build graph using ONLY the airways list!
                routingGraph.BuildGraph(airways, navTypeLookup);

                _logger.LogInformation($"Routing graph initialized with {routingGraph.GetAllNodes().Count()} nodes and {airways.Count} airways.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize routing graph.");
            }
        }

        // ====================================================================
        // VOR UTILITIES
        // ====================================================================
        public async Task<Vor?> GetClosestVorAsync(double lat, double lon, double minDistanceNm = 0)
        {
            double rangeDegrees = 2.0;

            double latMin = lat - rangeDegrees;
            double latMax = lat + rangeDegrees;
            double lonMin = lon - rangeDegrees;
            double lonMax = lon + rangeDegrees;

            var nearbyVors = await _context.Vors
                .Where(v => v.Laty >= latMin && v.Laty <= latMax && v.Lonx >= lonMin && v.Lonx <= lonMax).ToListAsync();

            if (!nearbyVors.Any()) return null;

            var bestVor = nearbyVors
                .Select(v => new
                {
                    Vor = v,
                    Distance = CalculateDistance(lat, lon, v.Laty, v.Lonx)
                })
                .Where(x => x.Distance >= minDistanceNm)
                .OrderBy(x => x.Distance)
                .FirstOrDefault();

            return bestVor?.Vor;
        }

        public async Task<Waypoint?> GetWaypointFromVorAsync(int vorId)
        {
            return await _context.Waypoints.FirstOrDefaultAsync(w => w.NavId == vorId);
        }

        // ====================================================================
        // MATH HELPER METHODS
        // ====================================================================
        public double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            var c = 2 * Math.Asin(Math.Min(1, Math.Sqrt(a)));

            return Math.Round(EarthRadiusNm * c, 1);
        }

        public TimeSpan CalculateEte(double distanceNm, int groundSpeedKts)
        {
            if (groundSpeedKts <= 0) return TimeSpan.Zero;

            double hours = distanceNm / (double)groundSpeedKts;
            TimeSpan timeSpan = TimeSpan.FromHours(hours);

            if (distanceNm < 500)
            {
                timeSpan = timeSpan.Add(TimeSpan.FromMinutes(15));
            }
            else if (distanceNm >= 500 && distanceNm < 1500)
            {
                timeSpan = timeSpan.Add(TimeSpan.FromMinutes(25));
            }
            else
            {
                timeSpan = timeSpan.Add(TimeSpan.FromMinutes(32));
            }

            return timeSpan;
        }

        public double CalculateBearing(double lat1, double lon1, double lat2, double lon2)
        {
            var dLon = ToRadians(lon2 - lon1);
            var y = Math.Sin(dLon) * Math.Cos(ToRadians(lat2));
            var x = Math.Cos(ToRadians(lat1)) * Math.Sin(ToRadians(lat2)) -
                    Math.Sin(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) * Math.Cos(dLon);

            var brng = Math.Atan2(y, x);
            return (ToDegrees(brng) + 360) % 360;
        }

        public double CalculateCrossTrackDistance(double startLat, double startLon, double endLat, double endLon, double pLat, double pLon)
        {
            double d13 = CalculateDistance(startLat, startLon, pLat, pLon) / EarthRadiusNm;
            double brng13 = ToRadians(CalculateBearing(startLat, startLon, pLat, pLon));
            double brng12 = ToRadians(CalculateBearing(startLat, startLon, endLat, endLon));

            double xtd = Math.Asin(Math.Sin(d13) * Math.Sin(brng13 - brng12));

            return Math.Abs(xtd * EarthRadiusNm);
        }
    }
}