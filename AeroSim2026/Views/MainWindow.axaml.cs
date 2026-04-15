using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Avalonia.Themes;
using ExCSS;
using System;

namespace AeroSim2026.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.Title = "AeroSim 2026 Flight Sim Tracker v0.0.25";
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            this.WindowState = WindowState.Maximized;

            SizeToContent = SizeToContent.Manual;
        }
        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
        private void TitleBar_PointerPressed(object sender, PointerPressedEventArgs e)
        {
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                this.BeginMoveDrag(e);
            }
        }
        // APP SHUTDOWN LOGIC
        public void App_Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
            ShutdownApp();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            ShutdownApp();
        }

        private void ShutdownApp()
        {
            // Avalonia apps don't have Application.Current.Shutdown().
            // You must cast the ApplicationLifetime to the desktop version.
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
            {
                lifetime.Shutdown();
            }
        }


        // 2. MAXIMIZE (Compatible logic, just ensure WindowState is from Avalonia.Controls)
        public void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Normal)
            {
                this.WindowState = WindowState.Maximized;
            }
            else
            {
                this.WindowState = WindowState.Normal;
            }
        }

        // 3. MINIMIZE (Compatible logic)
        public void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        // 4. MENU TOGGLE (Replaces ToggleButton_Click)
        private void ToggleButton_Click(object sender, RoutedEventArgs e)
        {
            // SAFEST FIX: Manually look up the control by its XAML name
            var menuPanel = this.FindControl<StackPanel>("MenuPanel");

            if (menuPanel != null)
            {
                menuPanel.IsVisible = !menuPanel.IsVisible;
            }
        }

        // 5. THEME SWITCHING (Replaces ThemeComboBox_SelectionChanged)
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
}