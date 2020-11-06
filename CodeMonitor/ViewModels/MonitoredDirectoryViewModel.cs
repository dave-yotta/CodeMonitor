using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using CodeMonitor.Models;
using DynamicData;
using ReactiveUI;

namespace CodeMonitor.ViewModels
{
    public class MonitoredDirectoryViewModel : ViewModelBase
    {
        public string Name { get; }

        private readonly ObservableAsPropertyHelper<bool> updating;
        public bool Updating => updating.Value;

        private readonly ObservableAsPropertyHelper<string> status;
        public string Status => status?.Value;

        public ReadOnlyObservableCollection<ProblemGroupViewModel> ProblemGroups => problemGroups;
        private readonly ReadOnlyObservableCollection<ProblemGroupViewModel> problemGroups;

        public ReadOnlyObservableCollection<FileToCleanViewModel> FilesToClean => filesToClean;
        private readonly ReadOnlyObservableCollection<FileToCleanViewModel> filesToClean;

        private readonly InspectCodeLoop _inspectLoop;
        private readonly CleanupCodeWatcher _cleanupWatcher;

        public ReactiveCommand<Unit, Unit> CleanFiles { get; }
        public ReactiveCommand<Unit, Unit> ResetCleanFiles { get; }

        public MonitoredDirectoryViewModel(string directory)
        {
            Name = Path.GetFileName(directory);

            var slns = Directory.GetFiles(directory, "*.sln").ToList();

            if (slns.Count == 1)
            {
                _inspectLoop = new InspectCodeLoop(slns[0]);
                _inspectLoop.Start();

                _inspectLoop.Problems
                            .Connect()
                            .Transform(x => new ProblemGroupViewModel(x))
                            .ObserveOn(RxApp.MainThreadScheduler)
                            .Bind(out problemGroups)
                            .DisposeMany()
                            .Subscribe();

                _cleanupWatcher = new CleanupCodeWatcher(slns[0]);
                _cleanupWatcher.Start();

                _cleanupWatcher.ToClean
                               .Connect()
                               .Transform(x => new FileToCleanViewModel(x.Path))
                               .ObserveOn(RxApp.MainThreadScheduler)
                               .Bind(out filesToClean)
                               .DisposeMany()
                               .Subscribe();

                status = _inspectLoop.Status.Merge(_cleanupWatcher.Status).ToProperty(this, x => x.Status);
                updating = _inspectLoop.Active.Merge(_cleanupWatcher.Active).ToProperty(this, x => x.Updating);

                CleanFiles = ReactiveCommand.CreateFromTask(() => Task.Run(() => _cleanupWatcher.CleanupFiles()));

                ResetCleanFiles = ReactiveCommand.CreateFromTask(() => Task.Run(() => _cleanupWatcher.ResetChanged()));
            }
        }
    }
}
