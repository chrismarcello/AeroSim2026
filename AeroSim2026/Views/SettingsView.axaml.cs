using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;

namespace AeroSim2026.Views;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
    }
    private void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox comboBox && comboBox.SelectedItem is ComboBoxItem selectedItem)
        {
            // Get the string content (Light, Dark, System)
            string? themeName = selectedItem.Content?.ToString();

            var app = Application.Current;
            if (app != null)
            {
                switch (themeName)
                {
                    case "Light":
                        app.RequestedThemeVariant = ThemeVariant.Light;
                        break;
                    case "Dark":
                        app.RequestedThemeVariant = ThemeVariant.Dark;
                        break;
                    default:
                        // 'Default' generally maps to the System preference
                        app.RequestedThemeVariant = ThemeVariant.Default;
                        break;
                }
            }
        }
    }
}