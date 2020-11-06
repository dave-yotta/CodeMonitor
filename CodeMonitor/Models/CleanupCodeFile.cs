namespace CodeMonitor.Models
{
    public class CleanupCodeFile
    {
        public CleanupCodeFile(string path)
        {
            Path = path;
        }

        public string Path { get; }
    }
}
