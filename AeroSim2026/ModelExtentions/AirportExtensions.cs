using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace AeroSim2026.EFModels
{
    public partial class Airport
    {
        public string DisplayName => $"{AirportName} ({Ident})";

        public string DisplayLocation
        {
            get
            {
                var parts = new List<string?>
                {
                    AirportsLocation?.GeoCity?.Name,   // Note the prefix!
                    AirportsLocation?.GeoAdmin2?.Name,
                    AirportsLocation?.GeoAdmin1?.Name,
                    AirportsLocation?.GeoCountry?.Name
                };

                return string.Join(", ", parts.Where(p => !string.IsNullOrWhiteSpace(p)));
            }
        }
    }
}
