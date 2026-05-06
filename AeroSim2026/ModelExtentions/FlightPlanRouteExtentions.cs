using System.ComponentModel.DataAnnotations.Schema;

namespace AeroSim2026.EFModels
{
    public partial class FlightPlanRoute
    {
        [NotMapped]
        public double? PlannedAltitude { get; set; }
    }
}
