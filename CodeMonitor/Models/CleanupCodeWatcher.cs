using System;
using System.Linq;
using System.Xml.Linq;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Text;
using System.Reactive.Subjects;
using System.Reactive.Linq;
using System.Reactive.Concurrency;
using DynamicData;

namespace CodeMonitor
{
    public class CleanupCodeWatcher
    {
        public CleanupCodeWatcher(string slnPath)
        {
            Watch = Path.GetDirectoryName(slnPath);
            Sln = Path.GetFileName(slnPath);
        }

        private readonly SourceList<CleanupCodeFile> results = new SourceList<CleanupCodeFile>();
        public IObservableList<CleanupCodeFile> ToClean => results;

        private readonly ReplaySubject<bool> active = new ReplaySubject<bool>(1);
        public IObservable<bool> Active => active;

        private readonly ReplaySubject<string> status = new ReplaySubject<string>();
        public IObservable<string> Status => status;

        private string Watch { get; }
        private string Sln { get; }

        private readonly object _mutex = new object();
        private readonly HashSet<string> _changed = new HashSet<string>();

        private void Handle(string s)
        {
            if (ShouldHandle(s))
            {
                lock (_mutex)
                {
                    var path = Path.GetRelativePath(Watch, s).Replace('\\', '/');
                    if (_changed.Add(path))
                    {
                        results.Add(new CleanupCodeFile(path));
                    }
                }
            }
        }

        private bool ShouldHandle(string s)
        {
            if (s.StartsWith(Path.Combine(Watch, ".git")) ||
                s.StartsWith(Path.Combine(Watch, ".vs")) ||
                s.EndsWith("~") ||
                s.EndsWith(".csproj") ||
                s.EndsWith(".TMP")
            ) return false;

            if (Directory.Exists(s))
            {
                return false;
            }

            var gitPath = new Uri(Watch).MakeRelativeUri(new Uri(s)).OriginalString;
            var proc = Process.Start(new ProcessStartInfo
            {
                WorkingDirectory = Watch,
                FileName = "git",
                Arguments = "check-ignore " + gitPath,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            });
            var result = proc.StandardOutput.ReadToEnd();
            proc.WaitForExit();

            if (result != "") return false;

            return true;
        }

        public void ResetChanged()
        {
            status.OnNext("Reseting cleanup tally against origin/dev");

            var proc = Process.Start(new ProcessStartInfo
            {
                WorkingDirectory = Watch,
                FileName = "git",
                Arguments = "diff origin/dev --name-only",
                RedirectStandardOutput = true,
                CreateNoWindow = true
            });
            var result = proc.StandardOutput.ReadToEnd();
            proc.WaitForExit();

            lock (_mutex)
            {
                results.Clear();
                _changed.Clear();

                var files = result.Split("\n", StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim('\r'));

                _changed.UnionWith(files);
                results.AddRange(files.Select(x => new CleanupCodeFile(x)));
            }
        }

        public void CleanupFiles()
        {
            var psi = new ProcessStartInfo
            {
                FileName = @"c:\bin\cleanupcode.exe",
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            var config = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<CleanupCodeOptions xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">
  <Debug>false</Debug>
  <Verbosity>INFO</Verbosity>
  <SolutionFile>{Path.Combine(Watch, Sln)}</SolutionFile>
  <CustomSettingFile>{Path.Combine(Watch, Sln + ".DotSettings")}</CustomSettingFile>
  <DisabledSettingsLayers />
  <SuppressBuildInSettings>false</SuppressBuildInSettings>
  <Properties />
  <TargetsForItems />
  <Extensions />
  <CleanupProfileName>Built-in: Reformat Code</CleanupProfileName>
  <CleanupScopeInclude>{string.Join(";", _changed)}</CleanupScopeInclude>
</CleanupCodeOptions>";

            File.WriteAllText("cleanupcode.config", config);

            psi.ArgumentList.Add("--config=cleanupcode.config");

            status.OnNext("Starting cleanup");
            var proc = Process.Start(psi);
            var opt = new StringBuilder();
            proc.OutputDataReceived += (o, e) => { opt.AppendLine(e.Data); status.OnNext(e.Data); };
            proc.ErrorDataReceived += (o, e) => { opt.AppendLine(e.Data); status.OnNext(e.Data); };
            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();
            proc.WaitForExit();
            if (proc.ExitCode != 0)
            {
                throw new Exception("CleanupCode was sad");
            }

            lock(_mutex)
            {
                _changed.Clear();
                results.Clear();
            }
        }

        private FileSystemWatcher w;

        internal void Stop()
        {
            w.Dispose();
        }

        public void Start()
        {
            // watch

            w = new FileSystemWatcher(Watch)
            {
                IncludeSubdirectories = true,
                EnableRaisingEvents = true,
                NotifyFilter = NotifyFilters.Attributes | NotifyFilters.CreationTime | NotifyFilters.DirectoryName | NotifyFilters.FileName | NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.Security | NotifyFilters.Size,
                Filter = "*.*"
            };

            w.Changed += (o, e) => Handle(e.FullPath);
            w.Created += (o, e) => Handle(e.FullPath);
            w.Renamed += (o, e) => Handle(e.FullPath);

            ResetChanged();
        }

    }
}
