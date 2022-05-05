using DynamicData;
using System.Collections.Generic;

namespace CodeMonitor.ViewModels
{
    public class ClimateGroupViewModel
    {
        public ClimateGroupViewModel(List<ClimateProblemViewModel> problems, string key)
        {
            Problems = problems;
            Key = key;
        }
        public string Key { get; }
        public List<ClimateProblemViewModel> Problems { get; }
    }
}
