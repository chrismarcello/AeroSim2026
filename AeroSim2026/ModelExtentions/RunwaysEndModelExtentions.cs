using System;
using System.Collections.Generic;
using System.Text;

namespace AeroSim2026.EFModels
{
    public partial class RunwaysEnd
    {
        public virtual ICollection<Runway> PrimaryRunways { get; set; } = new List<Runway>();
        public virtual ICollection<Runway> SecondaryRunways { get; set; } = new List<Runway>();
    }
}
