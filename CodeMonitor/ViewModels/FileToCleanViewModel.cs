namespace CodeMonitor.ViewModels
{
    public class FileToCleanViewModel
    {
        public FileToCleanViewModel(string path)
        {
            Path = path;
        }

        public string Path {get;}
    }
}
