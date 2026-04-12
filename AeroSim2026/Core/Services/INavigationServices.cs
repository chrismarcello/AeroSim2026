using AeroSim2026.EFModels;
using AeroSim2026.Models;
using AeroSim2026.Core.Routing;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AeroSim2026.Core.Services
{
    public interface INavigationServices
    {
        // Calculates Great Circle distance in Nautical Miles
        Task<Vor?> GetClosestVorAsync(double lat, double lon, double minDistanceNm = 0);
        Task<Waypoint?> GetWaypointFromVorAsync(int vorId);
                
        Task InitializeRoutingGraphAsync(RoutingGraph routingGraph);

        // --- Math Helpers ---
        double CalculateDistance(double lat1, double lon1, double lat2, double lon2);
        double CalculateBearing(double lat1, double lon1, double lat2, double lon2);
        double CalculateCrossTrackDistance(double startLat, double startLon, double endLat, double endLon, double pLat, double pLon);
        TimeSpan CalculateEte(double distanceNm, int groundSpeedKts);
    }
}
