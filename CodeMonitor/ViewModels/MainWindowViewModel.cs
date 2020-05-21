using System.Reactive;
using ReactiveUI;

namespace CodeMonitor.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private ObservableAsPropertyHelper<string> data;
        public string Data => data?.Value ?? "Awaiting input...";

        public ReactiveCommand<string, Unit> LoadSln { get; }

        private InspectCodeLoop _inspectLoop;

        public MainWindowViewModel()
        {
            LoadSln = ReactiveCommand.CreateFromTask<string, Unit>(async slnPath =>
            {
                data?.Dispose();
                _inspectLoop?.Stop();

                _inspectLoop = new InspectCodeLoop(slnPath);
                _inspectLoop.Start();

                data = _inspectLoop.Data.ToProperty(this, x => x.Data);
                this.RaisePropertyChanged(nameof(Data));

                return Unit.Default;
            });
        }
    }
}
