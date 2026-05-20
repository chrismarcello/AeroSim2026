using ReactiveUI;
using System.Reactive;

namespace AeroSim2026.ViewModels
{
    public class AddManufacturerViewModel : ViewModelBase
    {
        private string _manufacturerName = string.Empty;
        public string ManufacturerName
        {
            get => _manufacturerName;
            set => this.RaiseAndSetIfChanged(ref _manufacturerName, value);
        }

        public ReactiveCommand<Unit, string> SaveCommand { get; }
        public ReactiveCommand<Unit, string> CancelCommand { get; }

        public AddManufacturerViewModel()
        {
            var canSave = this.WhenAnyValue(x => x.ManufacturerName, name => !string.IsNullOrWhiteSpace(name));

            SaveCommand = ReactiveCommand.Create(() => ManufacturerName, canSave);
            CancelCommand = ReactiveCommand.Create(() => (string?)null)!;
        }
    }
}
