using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using Avalonia.Controls;
using CodeMonitor.Models;
using DynamicData;
using PropertyChanged;
using ReactiveUI;

namespace CodeMonitor.ViewModels
{
    [AddINotifyPropertyChangedInterface]
    public class MainWindowViewModel : ViewModelBase
    {
        public ReactiveCommand<Window,Unit> Add { get; }

        public ReadOnlyObservableCollection<MonitoredDirectoryViewModel> Monitored => monitored;
        private readonly ReadOnlyObservableCollection<MonitoredDirectoryViewModel> monitored;

        private readonly SourceCache<MonitoredDirectoryViewModel, string> monitoredSourceCache;

        public ReactiveCommand<Window, Unit> SetResharperCliPath {get; }

        public MainWindowViewModel()
        {
            monitoredSourceCache = new SourceCache<MonitoredDirectoryViewModel, string>(x => x.Name);

            monitoredSourceCache.Connect()
                                .ObserveOn(RxApp.MainThreadScheduler)
                                .Bind(out monitored)
                                .DisposeMany()
                                .Subscribe();

            Add = ReactiveCommand.CreateFromTask<Window>(async w =>
            {
                var d = new OpenFolderDialog();

                var result = await d.ShowAsync(w).ConfigureAwait(false);

                if (Directory.Exists(result))
                {
                    if (monitoredSourceCache.Keys.Any(x => x.Equals(result)))
                    {
                        throw new Exception("Already got it");
                    }

                    monitoredSourceCache.AddOrUpdate(new MonitoredDirectoryViewModel(result));
                }
            });

            SetResharperCliPath = ReactiveCommand.Create<Window>(async w =>
            {
                var d = new OpenFolderDialog ();

                Settings.RsCltPath = await d.ShowAsync(w).ConfigureAwait(false);
            });
        }
    }
}
