namespace CodeMonitor.ViewModels
{

    public class ProblemViewModel : ViewModelBase
    {
        public ProblemViewModel(string file, string message, int line, string type)
        {
            File = file;
            Message = message;
            Line = line;
            Type = type;
        }

        public string File { get; }
        public string Message { get; }
        public int Line { get; }
        public string Type { get; }
    }
}
