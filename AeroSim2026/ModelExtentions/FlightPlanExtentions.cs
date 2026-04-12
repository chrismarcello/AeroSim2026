using System;
using System.Collections.Generic;
using System.Text;

namespace AeroSim2026.EFModels
{
    public partial class FlightPlan
    {
        public string DisplayEstTime => EstFlightTime.HasValue ? $"{(int)EstFlightTime.Value.TotalHours}h {EstFlightTime.Value.Minutes}m" : "N/A";
    }
}
