namespace AeroSim2026.Models
{
    public class UserSettings
    {
        public string Theme { get; set; } = "System"; // Default to system theme
        public string FmsFolderPath { get; set; } = string.Empty;
        public string CustomDatabasePath { get; set; } = string.Empty;
    }
}
