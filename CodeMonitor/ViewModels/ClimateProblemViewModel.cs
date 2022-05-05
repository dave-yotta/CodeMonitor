namespace CodeMonitor.ViewModels
{
    public class ClimateProblemViewModel
    {
        public ClimateProblemViewModel(string path, string category, string text, double debt)
        {
            Path = path;
            Category = category;
            Text = text;
            Debt = debt;
        }

        public string Path { get; }
        public string Category { get; }
        public string Text { get; }
        public double Debt { get; }
    }
}
