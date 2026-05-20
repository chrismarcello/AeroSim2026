using AeroSim2026.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ReactiveUI.Avalonia;
using ReactiveUI;
using System;

namespace AeroSim2026.Views;

public partial class AddManufacturerWindow : ReactiveWindow<AddManufacturerViewModel>
{
    public AddManufacturerWindow()
    {
        InitializeComponent();

        this.WhenActivated(action =>
        {
            action(ViewModel!.SaveCommand.Subscribe(_ => Close()));
            action(ViewModel!.CancelCommand.Subscribe(_ => Close()));
        });
    }
}