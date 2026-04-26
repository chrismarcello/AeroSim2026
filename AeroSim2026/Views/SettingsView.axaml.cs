using AeroSim2026.Models;
using AeroSim2026.ViewModels;
using AeroSim2026.Core.Services;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using System.Linq;

namespace AeroSim2026.Views;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
    }
 
    private async void BrowseFmsFolder_Click(object sender, RoutedEventArgs e)
    {
        var desktop = Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
        if (desktop?.MainWindow == null) return;

        var folders = await desktop.MainWindow.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select X-Plane or MSFS FMS Folder",
            AllowMultiple = false
        });

        if (folders.Count >= 1 && DataContext is SettingsViewModel vm)
        {
            vm.FmsFolderPath = folders[0].Path.LocalPath;
        }
    }

    private async void BrowseDatabase_Click(object sender, RoutedEventArgs e)
    {
        var desktop = Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
        if (desktop?.MainWindow == null) return;

        var folders = await desktop.MainWindow.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select Custom Database Folder",
            AllowMultiple = false
        });

        if (folders.Count >= 1 && DataContext is SettingsViewModel vm)
        {
            vm.CustomDatabasePath = folders[0].Path.LocalPath;
        }
    }
}