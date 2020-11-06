using System.Collections.Generic;

namespace CodeMonitor.Models
{
    public class InspectCodeFileProblems
    {
        public InspectCodeFileProblems(string file, List<InspectCodeProblem> problems)
        {
            File = file;
            Problems = problems;
        }

        public string File { get; }
        public List<InspectCodeProblem> Problems { get; }
    }
}
