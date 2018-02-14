using System;
using System.Linq;
using System.Xml.Linq;
using Inedo.Extensibility.IssueSources;

namespace Inedo.Extensions.YouTrack
{
    public sealed class YouTrackIssue : IIssueTrackerIssue
    {
        internal YouTrackIssue(string baseUrl, XElement node)
        {
            this.Id = node?.Attribute("id")?.Value;
            this.Status = GetFieldValue(node, "Status");
            this.Type = GetFieldValue(node, "Type");
            this.Title = GetFieldValue(node, "summary");
            this.Description = GetFieldValue(node, "description");
            this.Submitter = GetFieldValue(node, "reporterFullName");
            this.SubmittedDate = ToDateTime(GetFieldValue(node, "created"));
            this.IsClosed = GetFieldValue(node, "resolved") != null;
            this.Url = baseUrl.TrimEnd('/') + "/issue/" + this.Id;
        }

        private static string GetFieldValue(XElement node, string name)
        {
            return node?.Elements("field")?.FirstOrDefault(f => f.Attribute("name")?.Value == name)?.Element("value")?.Value;
        }

        private static DateTime ToDateTime(string unixMilliseconds)
        {
            var sinceEpoch = long.Parse(unixMilliseconds);
            return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(sinceEpoch);
        }

        public string Id { get; }
        public string Status { get; }
        public string Type { get; }
        public string Title { get; }
        public string Description { get; }
        public string Submitter { get; }
        public DateTime SubmittedDate { get; }
        public bool IsClosed { get; }
        public string Url { get; }
    }
}
