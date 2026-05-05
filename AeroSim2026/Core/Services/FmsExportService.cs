using AeroSim2026.EFModels;
using AeroSim2026.Models;
using AeroSim2026.ViewModels;
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
                var cruiseAlt = flight.CruiseAltitude > 0 ? flight.CruiseAltitude : 5000;

                string fileName = $"{origin.Ident}_to_{dest.Ident}.fms";
                string fullPath = Path.Combine(folderPath, fileName);

                StringBuilder fmsBuilder = new StringBuilder();
                fmsBuilder.AppendLine("I");
                fmsBuilder.AppendLine("1100 Version");
                fmsBuilder.AppendLine("CYCLE 2406");
                fmsBuilder.AppendLine($"ADEP {origin.Ident}");
                fmsBuilder.AppendLine($"ADES {dest.Ident}");

                // Calculte total waypoints
                var routeList = routeItems?.ToList() ?? new List<FlightPlanRoute>();
                int totalWaypoints = routeList.Count + 2; // +2 for origin and destination

                fmsBuilder.AppendLine($"NUMENR {totalWaypoints}"); fmsBuilder.AppendLine($"1 {origin.Ident} ADEP {origin.Altitude:0.000000} {origin.Laty} {origin.Lonx}");

                foreach (var routeItem in routeList.OrderBy(r => r.SequenceNumber))
                {
                    if (routeItem.Waypoint != null)
                    {
                        fmsBuilder.AppendLine($"28 {routeItem.Waypoint.Ident} {routeItem.PlannedAltitude:0.000000} {routeItem.Waypoint.Laty} {routeItem.Waypoint.Lonx}");
                    }
                }

                fmsBuilder.AppendLine($"1 {dest.Ident} ADES {dest.Altitude:0.000000} {dest.Laty} {dest.Lonx}");

                File.WriteAllText(fullPath, fmsBuilder.ToString());
                return true;
            }
            catch (Exception ex) 
            {
                System.Diagnostics.Debug.WriteLine($"Failed to export FMS: {ex.Message}");
                return false;
            }
        }
    }
}
