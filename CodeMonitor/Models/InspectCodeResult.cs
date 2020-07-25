using System.Collections.Generic;

namespace CodeMonitor
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
