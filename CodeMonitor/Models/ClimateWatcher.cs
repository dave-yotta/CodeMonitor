using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Text;
using System.Reactive.Subjects;
using System.Reactive.Linq;
using DynamicData;
using RestSharp;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace CodeMonitor.Models
{
    public class ClimateWatcher
    {
        class ClimateResponse<T>
        {
            public List<T> Data { get; set; }
            public ClimatePageMeta Meta { get; set; }
        }

        class ClimateIssue
        {
            public ClimateIssueAttributes Attributes { get; set; }
        }

        class ClimatePageMeta
        {
            public int Current_Page { get; set; }
            public int Total_Pages { get; set; }
            public int Total_Count { get; set; }
        }

        class ClimateIssueAttributes
        {
            public string Check_Name { get; set; }
            public int Remediation_Points { get; set; }
            public ClimateIssueLocation Location { get; set; }
        }
        class ClimateIssueLocation
        {
            public string Path { get; set; }
        }

        class ClimateBuild
        {
            public ClimateAttributes Attributes { get; set; }
            public ClimateRelationship Relationships { get; set; }
        }

        class ClimateAttributes
        {
            public string Local_Ref { get; set; }
            public int Number { get; set; }
        }

        class ClimateRelationship
        {
            public ClimateSnapshotRel Snapshot { get; set; }
        }

        class ClimateSnapshotRel
        {
            public SnapshotRelData Data { get; set; }
        }

        class SnapshotRelData
        {
            public string Id { get; set; }
        }

        private readonly SourceList<ClimateProblem> results = new SourceList<ClimateProblem>();
        public IObservableList<ClimateProblem> Problems => results;

        public string Branch { get; }

        private readonly object _mutex = new object();

        private readonly ReplaySubject<bool> active = new ReplaySubject<bool>(1);
        public IObservable<bool> Active => active;

        private readonly ReplaySubject<string> status = new ReplaySubject<string>();
        public IObservable<string> Status => status;

        public async Task Query()
        {
            status.OnNext("Querying codeclimate");
            active.OnNext(true);

            var token = "6a53c699ebcb1470b8ee8b8ec6e689dbcb6f672f";
            var repo = "60880d08d29470125f00a1d5";

            var rc = new RestClient(new RestClientOptions("https://api.codeclimate.com/v1/"), x =>
            {
                x.Authorization = new AuthenticationHeaderValue("Token", "token=" + token);
            });

            var snapshotsRequest = new RestRequest($"repos/{repo}/builds")
                .AddQueryParameter("filter[local_ref]", "refs/heads/feature/test-code-climate");

            var resp = await rc.GetAsync<ClimateResponse<ClimateBuild>>(snapshotsRequest);
            var sid = resp.Data
                .OrderByDescending(x => x.Attributes.Number)
                .Select(x => x.Relationships.Snapshot.Data?.Id)
                .First(x => x != null);

            int page = 1, lastpage = int.MaxValue;

            results.Clear();
            while (page <= lastpage)
            {
                var issuesRequest = new RestRequest($"repos/{repo}/snapshots/{sid}/issues")
                    .AddQueryParameter("page[size]", "100")
                    .AddQueryParameter("page[number]", page)
                    .AddQueryParameter("filter[location.path]","AlloyEngine/CacheApis/CacheApi.cs");
                var issues = await rc.GetAsync<ClimateResponse<ClimateIssue>>(issuesRequest);
                status.OnNext($"Getting {issues.Meta.Total_Count} issues page {issues.Meta.Current_Page}/{issues.Meta.Total_Pages}");
                
                foreach (var issue in issues.Data)
                {
                    results.Add(new ClimateProblem(issue.Attributes.Check_Name, issue.Attributes.Remediation_Points, issue.Attributes.Location.Path));
                }

                lastpage = issues.Meta.Total_Pages;
                page++;
            }
            active.OnNext(false);
        }
    }
}
