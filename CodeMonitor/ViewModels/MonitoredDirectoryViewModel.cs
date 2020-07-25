using System;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using DynamicData;
using ReactiveUI;

#warning subscriptions need disposing
namespace CodeMonitor.ViewModels
{

    public class MonitoredDirectoryViewModel : ViewModelBase
    {
        public string Name { get; }

        private ObservableAsPropertyHelper<string> status;
        public string Status => status?.Value;

        public ReadOnlyObservableCollection<ProblemGroupViewModel> ProblemGroups => problemGroups;
        private ReadOnlyObservableCollection<ProblemGroupViewModel> problemGroups;

        private InspectCodeLoop _inspectLoop;

        public MonitoredDirectoryViewModel(string directory)
        {
            Name = Path.GetFileName(directory);

            var slns = Directory.GetFiles(directory, "*.sln").ToList();

            if (slns.Count == 1)
            {
                _inspectLoop = new InspectCodeLoop(slns[0]);
                _inspectLoop.Start();

                status = _inspectLoop.Status.ToProperty(this, x => x.Status);

                _inspectLoop.Problems
                            .TransformMany(x => x.Problems.Select(y => new ProblemViewModel(x.File, y.Message, y.Line, y.Type)), x => x.File)
                            .Group(x => x.File)
                            .Transform(x => new ProblemGroupViewModel(x))
                            .ObserveOn(RxApp.MainThreadScheduler)
                            .Bind(out problemGroups)
                            .Subscribe();
            }
        }
    }
}
