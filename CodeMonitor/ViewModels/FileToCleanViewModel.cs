using System;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using DynamicData;
using ReactiveUI;
using SharpDX.Direct2D1;

#warning subscriptions need disposing
namespace CodeMonitor.ViewModels
{
    public class FileToCleanViewModel
    {
        public FileToCleanViewModel(string path)
        {
            Path = path;
        }

        public string Path {get;}
    }
}
