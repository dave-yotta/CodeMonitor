using System;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using Avalonia.Controls;
using DynamicData;
using PropertyChanged;
using ReactiveUI;

#warning subscriptions need disposing
namespace CodeMonitor.ViewModels
{
    [AddINotifyPropertyChangedInterface]
    public class MainWindowViewModel : ViewModelBase
    {
        public ReactiveCommand<Window,Unit> Add { get; }

        public ReadOnlyObservableCollection<MonitoredDirectoryViewModel> Monitored => monitored;
        private ReadOnlyObservableCollection<MonitoredDirectoryViewModel> monitored;

        private SourceCache<MonitoredDirectoryViewModel, string> monitoredSourceCache;

        public MainWindowViewModel()
        {
            monitoredSourceCache = new SourceCache<MonitoredDirectoryViewModel, string>(x => x.Name);

            monitoredSourceCache.Connect()
                                .ObserveOn(RxApp.MainThreadScheduler)
                                .Bind(out monitored)
                                .Subscribe();

            Add = ReactiveCommand.CreateFromTask<Window,Unit>(async w =>
            {
                var d = new OpenFolderDialog();

                var result = await d.ShowAsync(w);

                if (Directory.Exists(result))
                {
                    if (monitoredSourceCache.Keys.Any(x => x.Equals(result)))
                    {
                        throw new Exception("Already got it");
                    }

                    monitoredSourceCache.AddOrUpdate(new MonitoredDirectoryViewModel(result));
                }
                return Unit.Default;
            });
        }
    }
}
