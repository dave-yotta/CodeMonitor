namespace CodeMonitor.Models
{
    public class InspectCodeProblem
    {
        public InspectCodeProblem(string message, int line, string type)
        {
            Message = message;
            Line = line;
            Type = type;
        }

        public string Message { get; }
        public int Line { get; }
        public string Type { get; }
    }
}
