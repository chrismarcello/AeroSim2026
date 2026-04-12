using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Text.Json;
using AeroSim2026.Models;

namespace AeroSim2026.EFModels
{
    public partial class SimAircraft
    {
        [NotMapped]
        public List<AircraftPropertyValues> Properties { get; set; } = new();

        public T? GetValue<T>(string propertyId)
        {
            // 1. Find the property in the list
            // We use StringComparison.OrdinalIgnoreCase so "cruise_speed" matches "Cruise_Speed"
            var prop = Properties.FirstOrDefault(p =>
                string.Equals(p.PropertyId, propertyId, StringComparison.OrdinalIgnoreCase));

            // 2. If not found, return default (null for objects, 0 for ints)
            if (prop == null || prop.PropertyValue == null)
            {
                return default;
            }

            // 3. The "Unboxing" Logic
            try
            {
                // CASE A: The types match exactly (e.g., PropertyValue is int, T is int)
                if (prop.PropertyValue is T directValue)
                {
                    return directValue;
                }

                // CASE B: Handling JSON Elements (Common if using System.Text.Json)
                // When deserializing object?, it often becomes a JsonElement.
                if (prop.PropertyValue is JsonElement jsonElement)
                {
                    // Convert the JsonElement to the target type T
                    return jsonElement.Deserialize<T>();
                }

                // CASE C: General Conversion (e.g., "140" string -> 140 int)
                // Convert.ChangeType is powerful but throws if the cast is invalid.
                // We use the underlying type if T is Nullable<int>
                var targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);

                return (T)Convert.ChangeType(prop.PropertyValue, targetType);
            }
            catch
            {
                // 4. Fail Safe: If conversion fails (e.g., "N/A" -> int), return default
                System.Diagnostics.Debug.WriteLine($"Failed to convert property '{propertyId}' value '{prop.PropertyValue}' to {typeof(T).Name}");
                return default;
            }
        }
    }
}
