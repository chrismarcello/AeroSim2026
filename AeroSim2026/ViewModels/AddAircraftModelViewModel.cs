using AeroSim2026.EFModels;
using AeroSim2026.Core.Services;
using ReactiveUI;
using System.Reactive;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace AeroSim2026.ViewModels
{
    public class AddAircraftModelViewModel : ViewModelBase
    {
        public string ParentTypeName { get; }
        public string Title => $"Add Model for {ParentTypeName}";

        private string _name = string.Empty;
        public string Name
        {
            get => _name;
            set => this.RaiseAndSetIfChanged(ref _name, value);
        }

        private string _nativeName = string.Empty;
        public string NativeName
        {
            get => _nativeName;
            set => this.RaiseAndSetIfChanged(ref _nativeName, value);
        }

        // Using int? to match the database's nullable requirement
        private int? _engineCount;
        public int? EngineCount
        {
            get => _engineCount;
            set => this.RaiseAndSetIfChanged(ref _engineCount, value);
        }

        private string _engineModels = string.Empty;
        public string EngineModels
        {
            get => _engineModels;
            set => this.RaiseAndSetIfChanged(ref _engineModels, value);
        }

        // The Commands that return our Tuple
        public ReactiveCommand<Unit, (string, string, int?, string)?> SaveCommand { get; }
        public ReactiveCommand<Unit, (string, string, int?, string)?> CancelCommand { get; }

        public AddAircraftModelViewModel(string parentTypeName)
        {
            ParentTypeName = parentTypeName;

            // Simple validation: Ensure they at least provide a Name before saving
            var canSave = this.WhenAnyValue(
                x => x.Name,
                name => !string.IsNullOrWhiteSpace(name));

            SaveCommand = ReactiveCommand.Create(() =>
                ((string, string, int?, string)?)(Name, NativeName, EngineCount, EngineModels),
                canSave);

            CancelCommand = ReactiveCommand.Create(() => ((string, string, int?, string)?)null);
        }
    }
}
