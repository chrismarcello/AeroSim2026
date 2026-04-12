using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AeroSim2026.EFModels;

namespace AeroSim2026.Models
{
    [Table("aircraft_properties")]
    public class PropertyDefinitionEntity
    {
        [Key]
        [Column("propertyId")] // Maps to propertyId
        public string PropertyId { get; set; }

        [Column("propertyName")]
        public string PropertyName { get; set; } // e.g. "Max. takeoff hp"

        [Column("unit")]
        public string Unit { get; set; }         // e.g. "horsepower"

        [Column("propertyType")]
        public string PropertyType { get; set; } // e.g. "integer", "float"

        [Column("description")]
        public string? Description { get; set; }
    }
}
