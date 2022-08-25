using System;
using System.Text.Json.Nodes;
using Inedo.Extensibility.IssueSources;

namespace Inedo.Extensions.YouTrack
{
    internal sealed class YouTrackIssue : IIssueTrackerIssue
    {
        private readonly string status;

        public YouTrackIssue(string id, JsonObject obj, string url, string status, string type)
        {
            this.ReadableId = id;
            this.Summary = (string)obj["summary"];
            this.Description = (string)obj["description"];
            this.Reporter = (string)(obj["reporter"] as JsonObject)?["fullName"];
            this.Created = ReadTimestamp((long?)obj["created"]);
            this.Resolved = ReadTimestamp((long?)obj["resolved"]);
            this.Url = url;
            this.status = status;
            this.Type = type ?? "Issue";
        }

        public string ReadableId { get; }
        public string Summary { get; }
        public string Description { get; }
        public string Reporter { get; }
        public DateTime? Created { get; }
        public DateTime? Resolved { get; }
        public string Url { get; }
        public string Status => this.status ?? (this.Resolved.HasValue ? "Closed" : "Open");
        public string Type { get; }

        string IIssueTrackerIssue.Id => this.ReadableId;
        string IIssueTrackerIssue.Title => this.Summary;
        string IIssueTrackerIssue.Description => this.Description;
        string IIssueTrackerIssue.Submitter => this.Reporter;
        DateTime IIssueTrackerIssue.SubmittedDate => this.Created.GetValueOrDefault();
        bool IIssueTrackerIssue.IsClosed => this.Resolved.HasValue;

        private static DateTime? ReadTimestamp(long? value) => value.HasValue ? new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddTicks(value.GetValueOrDefault() * TimeSpan.TicksPerMillisecond) : null;
    }
}
