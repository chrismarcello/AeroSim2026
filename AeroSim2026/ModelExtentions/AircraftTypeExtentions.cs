using System;
using System.Collections.Generic;
using System.Text;

namespace AeroSim2026.EFModels
{
    public partial class AircraftType
    {
        public string AircraftTypeDisplayName => $"{AircraftTypeName} ({IcaoCode})";
    }
}
