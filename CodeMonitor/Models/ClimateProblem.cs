namespace CodeMonitor.Models
{
    public class ClimateProblem
    {
        public ClimateProblem(string type, int points, string path)
        {
            Type = type;
            Points = points;
            Path = path;
        }

        public string Type { get; }
        public int Points { get; }
        public string Path { get; }
    }
}
