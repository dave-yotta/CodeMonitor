using System;

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

        public override bool Equals(object obj)
        {
            return obj is ProblemViewModel model &&
                   File == model.File &&
                   Message == model.Message &&
                   Line == model.Line &&
                   Type == model.Type;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(File, Message, Line, Type);
        }
    }
}
