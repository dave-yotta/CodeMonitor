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

namespace CodeMonitor.Models
{
    public class InspectCodeLoop
    {
        public InspectCodeLoop(string slnPath)
        {
            Watch = Path.GetDirectoryName(slnPath);
            Sln = Path.GetFileName(slnPath);
        }

        private readonly SourceCache<InspectCodeFileProblems, string> results = new SourceCache<InspectCodeFileProblems, string>(x => x.File);
        public IObservableCache<InspectCodeFileProblems, string> Problems => results;

        private readonly ReplaySubject<bool> active = new ReplaySubject<bool>(1);
        public IObservable<bool> Active => active;

        private readonly ReplaySubject<string> status = new ReplaySubject<string>();
        public IObservable<string> Status => status;

        private string Watch { get; }
        private string Sln { get; }

        private readonly object _mutex = new object();
        private readonly HashSet<string> _changed = new HashSet<string>();
        private readonly HashSet<string> _examined = new HashSet<string>();

        public void Handle(string s)
        {
            if (ShouldHandle(s))
            {
                lock (_mutex)
                {
                    _changed.Add(s);
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
            )
            {
                return false;
            }

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

            return result?.Length == 0;
        }

        private void ExamineFiles(ICollection<string> filePaths)
        {
            var outFile = Path.GetFullPath("resharper.out");

            var psi = new ProcessStartInfo
            {
                FileName = $@"{Settings.RsCltPath}\inspectcode.exe",
                CreateNoWindow = true,
                RedirectStandardOutput = true
            };

            var args = new List<string>
            {
                "--properties=Configuration=Debug;Platform=x64",
                $"--output={outFile}",
                "--severity=WARNING",
                "--format=Xml",
            };

            var tryProfile = Path.Combine(Watch, Sln + ".DotSettings");
            if (File.Exists(tryProfile))
            {
                args.Add($"--profile={tryProfile}");
            }

            //TODO: Use a config file
            if (filePaths.Count < 100)
            {
                args.AddRange(filePaths.Select(x => $"--input={x}"));
            }

            args.Add($"{Path.Combine(Watch, Sln)}");

            args.ForEach(psi.ArgumentList.Add);

            var proc = Process.Start(psi);
            proc.OutputDataReceived += (o, e) => status.OnNext(e.Data);
            proc.BeginOutputReadLine();
            proc.WaitForExit();
            var xml = File.ReadAllText(outFile);

            var doc = XDocument.Parse(xml);

            var problems = doc.Root.Element("Issues")
                                   .Elements("Project")
                                   .Elements("Issue")
                                   .Select(x =>
                                   (
                                       File: x.Attribute("File").Value,
                                       Message: x.Attribute("Message").Value,
                                       Line: int.Parse(x.Attribute("Line")?.Value ?? "0"),
                                       Type: x.Attribute("TypeId").Value
                                   ))
                                   .GroupBy(x => x.File)
                                   .ToDictionary(x => x.Key, x => x.Select(y => new InspectCodeProblem(y.Message, y.Line, y.Type)));

            var kees = results.Keys.Except(problems.Keys).ToList();

            results.RemoveKeys(kees);

            foreach (var x in problems)
            {
                _examined.Add(x.Key);
                results.AddOrUpdate(new InspectCodeFileProblems(x.Key, x.Value.ToList()));
            }
        }

        private IDisposable subscription;

        internal void Stop()
        {
            subscription.Dispose();
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

            status.OnNext("Doing Initial Analysis");
            subscription = Observable.Timer(TimeSpan.Zero, TimeSpan.FromSeconds(10))
                      .SubscribeOn(TaskPoolScheduler.Default)
                      .Subscribe(Loop);
        }

        private bool initial = true;
        private FileSystemWatcher w;

        private void Loop(long _)
        {
            // initial
            if (initial)
            {
                active.OnNext(true);
                ExamineFiles(new List<string>());
                initial = false;
                active.OnNext(false);
                return;
            }

            lock (_mutex)
            {
                var gone = _examined.Select(key => (key, full: Path.Combine(Watch, key)))
                                    .Where(x => !File.Exists(x.full) && ShouldHandle(x.full))
                                    .Select(x => x.key)
                                    .ToList();

                if (gone.Count > 0 || _changed.Count > 0)
                {
                    var changeNote = new StringBuilder();

                    changeNote.AppendLine();
                    changeNote.AppendLine("Changes detected!");
                    if (gone.Count > 0)
                    {
                        changeNote.AppendLine("Gone: ");
                        changeNote.AppendLine(string.Join(Environment.NewLine, gone));
                    }
                    if (_changed.Count > 0)
                    {
                        changeNote.AppendLine("Changed: ");
                        changeNote.AppendLine(string.Join(Environment.NewLine, _changed));
                    }
                    changeNote.AppendLine("Analyzing...");

                    foreach (var g in gone)
                    {
                        results.Remove(g);
                    }

                    active.OnNext(true);
                    status.OnNext(changeNote.ToString());
                    try
                    {
                        ExamineFiles(_changed);
                        _changed.Clear();
                    }
                    finally
                    {
                        status.OnNext("Idle");
                        active.OnNext(false);
                    }
                }
            }
        }
    }
}
