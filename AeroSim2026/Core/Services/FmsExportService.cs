using AeroSim2026.Core.Routing; // Required to access FlightRouteBuilder and VNAV math
using AeroSim2026.EFModels;
using AeroSim2026.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace AeroSim2026.Core.Services
{
    public class FmsExportService
    {
        public static bool ExportToXPlane(FlightPlan flight, IEnumerable<FlightPlanRoute> routeItems, string folderPath)
        {
            if (flight?.StartAirport == null || flight?.EndAirport == null) return false;

            try
            {
                var origin = flight.StartAirport;
                var dest = flight.EndAirport;

                // Safely extract the nullable integer into a strict int
                int cruiseAlt = (flight.CruiseAltitude.HasValue && flight.CruiseAltitude.Value > 0)
                                ? flight.CruiseAltitude.Value
                                : 5000;

                string fileName = $"{origin.Ident}_to_{dest.Ident}.fms";
                string fullPath = Path.Combine(folderPath, fileName);

                // 1. Create a LIST and STRICTLY ORDER IT before doing any math!
                var routeList = routeItems?.OrderBy(r => r.SequenceNumber).ToList() ?? new List<FlightPlanRoute>();
                int totalWaypoints = routeList.Count + 2;

                // 2. Always recalculate the altitudes right before export
                // (This guarantees the math is correct, even if not saved to the DB)
                if (routeList.Any())
                {
                    FlightRouteBuilder.ApplyVnavProfiles(origin, dest, routeList, cruiseAlt);
                }

                // 3. Build the FMS File
                StringBuilder fmsBuilder = new StringBuilder();
                fmsBuilder.AppendLine("I");
                fmsBuilder.AppendLine("1100 Version");
                fmsBuilder.AppendLine("CYCLE 2406");
                fmsBuilder.AppendLine($"ADEP {origin.Ident}");
                fmsBuilder.AppendLine($"ADES {dest.Ident}");
                fmsBuilder.AppendLine($"NUMENR {totalWaypoints}");
                fmsBuilder.AppendLine($"1 {origin.Ident} ADEP {origin.Altitude:0.000000} {origin.Laty} {origin.Lonx}");

                // Use a for-loop so we can look ahead to the next waypoint!
                for (int i = 0; i < routeList.Count; i++)
                {
                    var routeItem = routeList[i];
                    if (routeItem.Waypoint != null)
                    {
                        string airwayName = "DRCT";

                        // THE FIX: Look ahead to the NEXT waypoint to get the DEPARTURE airway
                        if (i < routeList.Count - 1)
                        {
                            var nextItem = routeList[i + 1];
                            if (nextItem.Airway != null && !string.IsNullOrWhiteSpace(nextItem.Airway.AirwayName))
                            {
                                airwayName = nextItem.Airway.AirwayName;
                            }
                        }

                        int typeId = 11; // Default to Database Intersection/FIX
                        string wpType = routeItem.Waypoint.WaypointType?.ToUpper() ?? "";

                        if (wpType.Contains("VOR")) typeId = 3;
                        else if (wpType.Contains("NDB")) typeId = 2;
                        else if (wpType.Contains("GPS") || wpType.Contains("LATLON")) typeId = 28;

                        fmsBuilder.AppendLine($"{typeId} {routeItem.Waypoint.Ident} {airwayName} {routeItem.PlannedAltitude:0.000000} {routeItem.Waypoint.Laty} {routeItem.Waypoint.Lonx}");
                    }
                }

                fmsBuilder.AppendLine($"1 {dest.Ident} ADES {dest.Altitude:0.000000} {dest.Laty} {dest.Lonx}");

                File.WriteAllText(fullPath, fmsBuilder.ToString());
                return true;
            }
            catch (Exception ex)
            {
                // If it ever fails again, it will print the exact reason to your Visual Studio Output Window!
                System.Diagnostics.Debug.WriteLine($"Failed to export FMS: {ex.Message}");
                return false;
            }
        }
    }
}