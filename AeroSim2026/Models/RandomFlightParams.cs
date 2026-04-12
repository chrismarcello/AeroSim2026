using System;
using System.Collections.Generic;
using System.Text;

namespace AeroSim2026.Models
{
    public class RandomFlightParams
    {        
        public Int32 SimAircraftId { get; set; }
        public Int32 MaxRange { get; set; } = 500; // in nautical miles
        public double CruiseSpeed { get; set; } = 140.0; // in knots
        public String Continent { get; set; } = string.Empty;
        public String DepartureAirportIdent { get; set; } = string.Empty;
        public Int32 DepartureAirportId { get; set; } = 0;
        public Int32 DepartAirportTypeId { get; set; } = 0; // 1 = small, 2 = medium, 3 = large
        public Int32 ArrivalAirportTypeId { get; set; } = 0; // 1 = small, 2 = medium, 3 = large
        public Int32 MinRotateRunwayLength { get; set; } = 1500; // in feet
        public Int32 MinLandingRunwayLength { get; set; } = 1000; // in feet
        public UnitOfLength UnitOfLength { get; set; } = UnitOfLength.NauticalMiles;
        public double MinDistance { get; set; } = 50.0;
        public double MaxDistance { get; set; } = 0.00;
        public Coordinates? Coordinates { get; set; }
        public bool HasIls { get; set; } = false;
        public bool IsMilitary { get; set; } = false;
        public int CruiseAltitude { get; set; } = 0;
    }
}
