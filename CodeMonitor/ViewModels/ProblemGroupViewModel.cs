using System.Collections.Generic;
using CodeMonitor.Models;

namespace CodeMonitor.ViewModels
{
    public class ProblemGroupViewModel : ViewModelBase
    {
        public ProblemGroupViewModel(InspectCodeFileProblems model)
        {
            Group = model.File;
            Problems = model.Problems.ConvertAll(y => new ProblemViewModel(model.File, y.Message, y.Line, y.Type));
        }

        public string Group { get; }
        public List<ProblemViewModel> Problems { get; }
    }
}
