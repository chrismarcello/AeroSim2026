using AeroSim2026.EFModels;
using AeroSim2026.Core.Services;
using ReactiveUI;
using System.Reactive;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace AeroSim2026.ViewModels
{
    public class AddAircraftTypeViewModel : ViewModelBase
    {
        private readonly IAircraftServices _aircraftServices;

        private string _aircraftTypeName = string.Empty;
        public string ManufacturerName { get; }
        public string AircraftTypeName
        {
            get => _aircraftTypeName;
            set => this.RaiseAndSetIfChanged(ref _aircraftTypeName, value);
        }

        // --- Input Properties ---
        private string _typeName = string.Empty;
        public string TypeName 
        { 
            get => _typeName; 
            set => this.RaiseAndSetIfChanged(ref _typeName, value); 
        }
        private string _typeCode = string.Empty;
        public string TypeCode
        {
            get => _typeCode;
            set => this.RaiseAndSetIfChanged(ref _typeCode, value);
        }
        private string? _selectedAircraftFamily;
        public string? SelectedAircraftFamily
        {
            get => _selectedAircraftFamily; 
            set => this.RaiseAndSetIfChanged(ref _selectedAircraftFamily, value);
        }
        private string? _selectedEngineFamily;
        public string? SelectedEngineFamily
        {
            get => _selectedEngineFamily; set => this.RaiseAndSetIfChanged(ref _selectedEngineFamily, value);
        }

        // --- Dropdown lists ---
        public ObservableCollection<string> AvailableAircraftFamilies { get; } = new();
        public ObservableCollection<string> AvailableEngineFamilies { get; } = new();

        public ReactiveCommand<Unit, (string Name, string Code, string AirFam, string EngFame)?> SaveCommand { get; }
        public ReactiveCommand<Unit, (string Name, string Code, string AirFam, string EngFame)?> CancelCommand { get; }
        public AddAircraftTypeViewModel(IAircraftServices aircraftServices, string manufacturerName)
        {
            _aircraftServices = aircraftServices;
            ManufacturerName = manufacturerName;

            var canSave = this.WhenAnyValue(
                x => x.TypeName,
                x => x.TypeCode,
                (name, code) => !string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(code));

            SaveCommand = ReactiveCommand.Create(() =>
            {
                return ((string, string, string, string)?)(
                    TypeName,
                    TypeCode,
                    SelectedAircraftFamily ?? "None",
                    SelectedEngineFamily ?? "None");
            }, canSave);

            CancelCommand = ReactiveCommand.Create(() => ((string, string, string, string)?)null);

            LoadFamiliesAsync();
        }

        private async void LoadFamiliesAsync()
        {
            var airFamilies = await _aircraftServices.GetDistinctAircraftFamiliesAsync();
            AvailableAircraftFamilies.Add("None");
            foreach (var f in airFamilies) AvailableAircraftFamilies.Add(f);
            SelectedAircraftFamily = "None";
            var engFamilies = await _aircraftServices.GetDistinctEngineFamiliesAsync();
            AvailableEngineFamilies.Add("None");
            foreach (var f in engFamilies) AvailableEngineFamilies.Add(f);
            SelectedEngineFamily = "None";
        }
    }
}
