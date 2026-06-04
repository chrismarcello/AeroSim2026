using AeroSim2026.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ReactiveUI;
using ReactiveUI.Avalonia;
using System;
using System.Reactive.Disposables.Fluent;

namespace AeroSim2026.Views;

public partial class AddAircraftTypeWindow : ReactiveWindow<AddAircraftTypeViewModel>
{
    public AddAircraftTypeWindow()
    {
        InitializeComponent();

        this.WhenActivated(d =>
        {
            // Explicitly pass the string result to Close() and dispose it safely
            ViewModel!.SaveCommand
                .Subscribe(result => Close(result))
                .DisposeWith(d);

            // Explicitly pass null to Close() when cancelling
            ViewModel!.CancelCommand
                .Subscribe(_ => Close(null))
                .DisposeWith(d);
        });
    }
}