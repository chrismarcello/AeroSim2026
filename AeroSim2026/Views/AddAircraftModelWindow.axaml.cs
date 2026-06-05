using AeroSim2026.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ReactiveUI;
using ReactiveUI.Avalonia;
using System;
using System.Reactive.Disposables.Fluent;

namespace AeroSim2026.Views;

public partial class AddAircraftModelWindow : ReactiveWindow<AddAircraftModelViewModel>
{
    public AddAircraftModelWindow()
    {
        InitializeComponent();

        this.WhenActivated(d =>
        {
            if (ViewModel != null)
            {
                // Subscribe to the commands. When they finish executing, close the window
                // and pass the Tuple result back to the Interaction caller.
                d(ViewModel.SaveCommand.Subscribe(result => Close(result)));
                d(ViewModel.CancelCommand.Subscribe(result => Close(result)));
            }
        });
    }
}