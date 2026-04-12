using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.ComponentModel.DataAnnotations.Schema; // 1. Add this using statement!

namespace AeroSim2026.EFModels
{
    public partial class AirportsLocation
    {
        // 2. Add the ForeignKey attributes mapping to the actual integer properties
        [ForeignKey(nameof(GeonameId))]
        public virtual GeonameEntity GeoPlace { get; set; }

        [ForeignKey(nameof(CityId))]
        public virtual GeonameEntity GeoCity { get; set; }

        [ForeignKey(nameof(CountryId))]
        public virtual GeonameEntity GeoCountry { get; set; }

        [ForeignKey(nameof(Admin1))]
        public virtual GeonameEntity GeoAdmin1 { get; set; }

        [ForeignKey(nameof(Admin2))]
        public virtual GeonameEntity GeoAdmin2 { get; set; }

        [ForeignKey(nameof(Admin3))]
        public virtual GeonameEntity GeoAdmin3 { get; set; }

        public string DisplayLocation
        {
            get
            {
                var parts = new List<string?>
                {
                    GeoCity?.Name,
                    //GeoAdmin3?.Name,
                    GeoAdmin2?.Name,
                    GeoAdmin1?.Name,
                    GeoCountry?.Name
                };
                return string.Join(", ", parts.Where(p => !string.IsNullOrWhiteSpace(p)));
            }
        }
    }
}