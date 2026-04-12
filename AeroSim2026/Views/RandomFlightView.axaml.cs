using AeroSim2026.EFModels;
using AeroSim2026.ViewModels;
using Avalonia.Controls;
using Avalonia.Data;
using Mapsui;
using Mapsui.UI.Avalonia;
using ReactiveUI;
using ReactiveUI.Avalonia;
using System.ComponentModel;
using System.Linq;

namespace AeroSim2026.Views
{
    public partial class RandomFlightView : ReactiveUserControl<RandomFlightViewModel>
    {
        public RandomFlightView()
        {
            InitializeComponent();
            InitializeMap();
        }

        private void InitializeMap()
        {
            // 1. Create the MapControl programmatically
            var mapControl = new MapControl();

            // 2. Bind its "Map" property to the nested "MapViewModel.Map" property
            mapControl.Bind(MapControl.MapProperty, new Binding("MapViewModel.Map"));

            // 3. Inject it into the UI Border named MapContainer
            var container = this.FindControl<Border>("MapContainer");
            if (container != null)
            {
                container.Child = mapControl;
            }
        }
    }
}