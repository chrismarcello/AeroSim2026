using ReactiveUI;

namespace AeroSim2026.Core.Services
{
    public class StatusService : ReactiveObject, IStatusService
    {
        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set => this.RaiseAndSetIfChanged(ref _isBusy, value);
        }
        private string _statusMessage = "Ready";
        public string StatusMessage
        {
            get => _statusMessage;
            set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
        }
    }
}
