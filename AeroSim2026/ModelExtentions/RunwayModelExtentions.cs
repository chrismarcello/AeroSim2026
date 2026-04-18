using System;
using System.Collections.Generic;
using System.Text;

namespace AeroSim2026.EFModels
{
    public partial class Runway
    {
        public virtual RunwaysEnd? SecondaryEnd { get; set; }

        public string DisplayIdent
        {
            get
            {
                // Null conditional operators (?) ensure the app doesn't crash 
                // if EF hasn't loaded the ends yet
                string primary = PrimaryEnd?.Name ?? "??";
                string secondary = SecondaryEnd?.Name ?? "??";

                return $"{primary}/{secondary}";
            }
        }
    }
}
