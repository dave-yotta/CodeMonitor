using System;
using System.Collections.Generic;
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

    public class ProblemGroupViewModel : ViewModelBase
    {
        public ProblemGroupViewModel(InspectCodeFileProblems model)
        {
            Group = model.File;
            Problems = model.Problems.Select(y => new ProblemViewModel(model.File, y.Message, y.Line, y.Type)).ToList();
        }

        public string Group { get; }
        public List<ProblemViewModel> Problems { get; }
    }
}
