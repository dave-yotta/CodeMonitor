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
using Avalonia.Controls;

namespace CodeMonitor
{
    public class InspectCodeLoop
    {
        public InspectCodeLoop(string slnPath)
        {
            Watch = Path.GetDirectoryName(slnPath);
            Sln = Path.GetFileName(slnPath);
        }

        private readonly ReplaySubject<string> data = new ReplaySubject<string>();
        public IObservable<string> Data => data;

        private string Watch { get; }
        private string Sln { get; }

        private readonly object _mutex = new object();
        private readonly HashSet<string> _changed = new HashSet<string>();
        private readonly Dictionary<string, List<(int line, string message)>> _problems = new Dictionary<string, List<(int line, string message)>>();

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
                CreateNoWindow=true
            });
            var result = proc.StandardOutput.ReadToEnd();
            proc.WaitForExit();

            if (result != "") return false;

            return true;
        }

        private string GetProblemsString()
        {
            var builder = new StringBuilder();

            if (!_problems.Any())
            {
                builder.AppendLine();
                builder.AppendLine("No problems!");
            }

            foreach (var problemSet in _problems)
            {
                if (problemSet.Value.Count > 1)
                {
                    builder.AppendLine($"{problemSet.Key}");
                    foreach (var problem in problemSet.Value)
                    {
                        builder.AppendLine($"   {problem.line}: {problem.message}");
                    }
                }
                else builder.AppendLine($"{problemSet.Key}:{problemSet.Value[0].line} {problemSet.Value[0].message}");
            }
            return builder.ToString();
        }

        private void UpdateProblems(string xml, List<string> examinedFiles)
        {
            examinedFiles.Select(_problems.Remove).ToArray();

            var doc = XDocument.Parse(xml);

            var data = doc.Root.Element("Issues")
                               .Elements("Project")
                               .Elements((XName)"Issue")
                               .Select(x => (
                                    Type: x.Attribute("TypeId").Value,
                                    File: x.Attribute("File").Value,
                                    Line: int.Parse(x.Attribute("Line")?.Value ?? "0"),
                                    Message: x.Attribute("Message").Value
                                ))
                                .ToList();

            data.Select(x => x.File).Distinct().ToList().ForEach(x => _problems[x] = new List<(int line, string message)>());

            foreach (var problem in data)
            {
                _problems[problem.File].Add((problem.Line, problem.Message));
            }
        }

        private string ExamineFiles(List<string> filePaths)
        {
            //return File.ReadAllText("resharper.out");

            var outFile = Path.GetFullPath("resharper.out");

            var psi = new ProcessStartInfo
            {
                FileName = @"c:\bin\inspectcode.exe",
                CreateNoWindow=true
            };

            var args = new List<string>
            {
                "--properties=Configuration=Debug;Platform=x64",
                $"--output={outFile}",
                "--severity=WARNING",
                "--format=Xml",
            };

            var tryProfile = Path.Combine(Watch, Sln + ".DotSettings");
            if(File.Exists(tryProfile))
            {
                args.Add($"--profile={tryProfile}");
            }

            args.AddRange(filePaths.Select(x => $"--input={x}"));
            args.Add($"{Path.Combine(Watch, Sln)}");

            args.ForEach(psi.ArgumentList.Add);

            var proc = Process.Start(psi);
            proc.WaitForExit();
            return File.ReadAllText(outFile);
        }

        IDisposable subscription;

        internal void Stop()
        {
            subscription.Dispose();
        }

        public void Start()
        {
            // watch

            var w = new FileSystemWatcher(Watch)
            {
                IncludeSubdirectories = true,
                EnableRaisingEvents = true,
                NotifyFilter = NotifyFilters.Attributes | NotifyFilters.CreationTime | NotifyFilters.DirectoryName | NotifyFilters.FileName | NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.Security | NotifyFilters.Size,
                Filter = "*.*"
            };

            w.Changed += (o, e) => Handle(e.FullPath);
            w.Created += (o, e) => Handle(e.FullPath);
            w.Renamed += (o, e) => Handle(e.FullPath);

            data.OnNext("Doing Initial Analysis");
            subscription = Observable.Timer(TimeSpan.Zero, TimeSpan.FromSeconds(10))
                      .SubscribeOn(TaskPoolScheduler.Default)
                      .Subscribe(Loop);
        }

        private bool initial = true;
        private string lastProblems = "";

        private void Loop(long _)
        {
            // initial
            if (initial)
            {
                var result = ExamineFiles(new List<string>());
                UpdateProblems(result, new List<string>());
                lastProblems = GetProblemsString();
                data.OnNext(lastProblems);
                initial = false;
                return;
            }
            
            lock (_mutex)
            {
                var gone = _problems.Keys
                                   .Select(key => (key, full: Path.Combine(Watch, key)))
                                   .Where(x => !File.Exists(x.full) && ShouldHandle(x.full))
                                   .Select(x => x.key)
                                   .ToList();

                if (gone.Any() || _changed.Any())
                {
                    var changeNote = new StringBuilder();

                    changeNote.Append(lastProblems);
                    changeNote.AppendLine();
                    changeNote.AppendLine("Changes detected!");
                    if (gone.Any())
                    {
                        changeNote.AppendLine("Gone: ");
                        changeNote.AppendLine(string.Join(Environment.NewLine, gone));
                    }
                    if (_changed.Any())
                    {
                        changeNote.AppendLine("Changed: ");
                        changeNote.AppendLine(string.Join(Environment.NewLine, _changed));
                    }
                    changeNote.AppendLine("Analyzing...");

                    data.OnNext(changeNote.ToString());

                    gone.Select(_problems.Remove).ToArray();

                    var changedList = _changed.ToList();

                    var result = ExamineFiles(changedList);
                    UpdateProblems(result, changedList);
                    lastProblems = GetProblemsString();
                    data.OnNext(lastProblems);

                    _changed.Clear();
                }
            }
        }
    }
}
