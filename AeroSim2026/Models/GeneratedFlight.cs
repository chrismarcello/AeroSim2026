using AeroSim2026.EFModels;
using System;
using System.Collections.Generic;
using System.Text;

namespace AeroSim2026.Models
{
    public class GeneratedFlight
    {
        public SimAircraft? Aircraft { get; set; }
        public Airport? OriginAirport { get; set; }
        public Airport? ArrivalAirport { get; set; }
        public double? DistanceNm { get; set; }
        public double? PlannedSpeed { get; set; } // in knots
        public TimeSpan? EstFlightTime { get; set; }
        public int? CruiseAltitude { get; set; } = 5000; // Default cruise altitude in feet
        //public FlightPlan? FlightPlan { get; set; }
    }
}
