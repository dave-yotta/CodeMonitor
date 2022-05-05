using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CodeMonitor.Models;
using DynamicData;
using DynamicData.Binding;
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

        public ReadOnlyObservableCollection<ClimateGroupViewModel> ClimateProblemGroups => climateProblemGroups;
        private readonly ReadOnlyObservableCollection<ClimateGroupViewModel> climateProblemGroups;

        public readonly ObservableCollection<string> ClimateProblemGroupings = new ObservableCollection<string> { "^AlloyEngine", "^Alloy.*Test/", ".*" };

        private readonly InspectCodeLoop _inspectLoop;
        private readonly CleanupCodeWatcher _cleanupWatcher;
        private readonly ClimateWatcher _climateWatcher;

        public ReactiveCommand<Unit, Unit> CleanFiles { get; }
        public ReactiveCommand<Unit, Unit> ResetCleanFiles { get; }
        public ReactiveCommand<Unit, Unit> QueryClimate { get; }

        public ReactiveCommand<string, Unit> AddClimateGrouping { get; }
        public ReactiveCommand<string, Unit> RemoveClimateGrouping { get; }

        public MonitoredDirectoryViewModel(string directory)
        {
            Name = Path.GetFileName(directory);

            var slns = Directory.GetFiles(directory, "*.sln").ToList();

            _climateWatcher = new ClimateWatcher();
            _climateWatcher.Problems
                           .Connect()
                           //.GroupOn(x => ClimateProblemGroupings.First(g => Regex.IsMatch(x.Path, g)), ClimateProblemGroupings.ToObservableChangeSet().Select(x => Unit.Default))
                           .GroupOn(x => x.Type)
                           .Transform(x => new ClimateGroupViewModel(x.List.Items.Select(x => new ClimateProblemViewModel(x.Path, x.Type, "?", x.Points)).ToList(), x.GroupKey))
                           .ObserveOn(RxApp.MainThreadScheduler)
                           .Bind(out climateProblemGroups)
                           .DisposeMany()
                           .Subscribe();

            QueryClimate = ReactiveCommand.CreateFromTask(() => Task.Run(_climateWatcher.Query));

            var statusO = _climateWatcher.Status;
            var updatingO = _climateWatcher.Active;

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

                statusO = statusO.Merge(_inspectLoop.Status).Merge(_cleanupWatcher.Status);
                updatingO = updatingO.Merge(_inspectLoop.Active).Merge(_cleanupWatcher.Active);

                CleanFiles = ReactiveCommand.CreateFromTask(() => Task.Run(_cleanupWatcher.CleanupFiles));

                ResetCleanFiles = ReactiveCommand.CreateFromTask(() => Task.Run(_cleanupWatcher.ResetChanged));

            }

            status = statusO.ToProperty(this, x => x.Status);
            updating = updatingO.ToProperty(this, x => x.Updating);
        }
    }
}
