using System;
using System.Collections.Generic;
using System.Text;


namespace AeroSim2026.EFModels
{
    public partial class AircraftModel
    {
        public string DisplayName =>
    $"{(ManufacturerNavigation?.ManufacturerName ?? "Unknown")} " +
    $"{AircraftName} " +
    $"({(AircraftTypeNavigation?.IcaoCode ?? "N/A")})";
    }
}
