using System;
using System.Collections.Generic;
using System.Text;

namespace AeroSim2026.Models
{
    public class RandomArrivalAirport
    {
        public int AirportId { get; set; }
        public double Distance { get; set; }
        public Coordinates? Coordinates { get; set; }
        public int TypeId { get; set; }
        public UnitOfLength? UnitOfLength { get; set; }
    }
}
