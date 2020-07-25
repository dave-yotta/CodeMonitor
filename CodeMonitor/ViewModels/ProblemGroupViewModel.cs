using System;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using DynamicData;
using ReactiveUI;

namespace CodeMonitor.ViewModels
{
#warning subscriptions need disposing

    public class ProblemGroupViewModel :ViewModelBase
    {
        public ProblemGroupViewModel(IGroup<ProblemViewModel, string, string> x)
        {
            Group = x.Key;
            x.Cache.Connect()
                   .ObserveOn(RxApp.MainThreadScheduler)
                   .Bind(out problems)
                   .Subscribe();
        }

        public string Group { get; }
        public ReadOnlyObservableCollection<ProblemViewModel> Problems => problems;
        private readonly ReadOnlyObservableCollection<ProblemViewModel> problems;
    }
}
