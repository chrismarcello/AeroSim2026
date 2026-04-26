using AeroSim2026.Core.Services;
using AeroSim2026.Models;
using Avalonia;
using Avalonia.Styling;
using ReactiveUI;
using System.Collections.Generic;
using System.Reactive;

namespace AeroSim2026.ViewModels
{
    public class SettingsViewModel : PageViewModelBase
    {
        public override string Title => "Settings";

        public List<string> AvailableThemes { get; } = new List<string> { "System", "Light", "Dark" };

        private string _selectedTheme = "System";
        public string SelectedTheme
        {
            get => _selectedTheme;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedTheme, value);
                // 2. Change the theme in real-time when the property updates!
                UpdateApplicationTheme(value);
            }
        }

        private string _fmsFolderPath = string.Empty;
        public string FmsFolderPath
        {
            get => _fmsFolderPath;
            set => this.RaiseAndSetIfChanged(ref _fmsFolderPath, value);
        }

        private string _customDatabasePath = string.Empty;
        public string CustomDatabasePath
        {
            get => _customDatabasePath;
            set => this.RaiseAndSetIfChanged(ref _customDatabasePath, value);
        }

        public ReactiveCommand<Unit, Unit> SaveSettingsCommand { get; }

        public SettingsViewModel()
        {
            var settings = UserSettingsService.LoadSettings();
            SelectedTheme = settings.Theme;
            FmsFolderPath = settings.FmsFolderPath;
            CustomDatabasePath = settings.CustomDatabasePath;
            
            SaveSettingsCommand = ReactiveCommand.Create(() =>
            {
                var newSettings = new UserSettings
                {
                    Theme = SelectedTheme,
                    FmsFolderPath = FmsFolderPath,
                    CustomDatabasePath = CustomDatabasePath
                };
                UserSettingsService.SaveSettings(newSettings);
            });
        }
        private void UpdateApplicationTheme(string themeName)
        {
            if (Application.Current == null) return;

            switch (themeName)
            {
                case "Light":
                    Application.Current.RequestedThemeVariant = ThemeVariant.Light;
                    break;
                case "Dark":
                    Application.Current.RequestedThemeVariant = ThemeVariant.Dark;
                    break;
                default:
                    Application.Current.RequestedThemeVariant = ThemeVariant.Default;
                    break;
            }
        }
    }
}