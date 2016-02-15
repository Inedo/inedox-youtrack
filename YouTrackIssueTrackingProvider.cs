using System;
using System.Linq;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Extensibility.Providers;
using Inedo.BuildMaster.Extensibility.Providers.IssueTracking;
using Inedo.BuildMaster.Web;

namespace Inedo.BuildMasterExtensions.YouTrack
{
    [ProviderProperties(
        "YouTrack",
        "Issue tracking provider for JetBrains YouTrack.")]
    [CustomEditor(typeof(YouTrackIssueTrackingProviderEditor))]
    public sealed class YouTrackIssueTrackingProvider : IssueTrackingProviderBase, ICategoryFilterable, IUpdatingProvider
    {
        internal const string AnyProjectCategory = "any project";

        private Lazy<YouTrackSession> session;

        public YouTrackIssueTrackingProvider()
        {
            this.MaxIssues = 50;
            this.ReleaseField = "Fix versions";
            this.session = new Lazy<YouTrackSession>(() => new YouTrackSession(this.BaseUrl, this.UserName, this.Password, this.ReleaseField));
        }

        [Persistent]
        public string BaseUrl { get; set; }
        [Persistent]
        public string UserName { get; set; }
        [Persistent]
        public string Password { get; set; }
        [Persistent]
        public string ReleaseField { get; set; }
        [Persistent]
        public int MaxIssues { get; set; }
        [Persistent]
        public string[] CategoryIdFilter { get; set; }
        public string[] CategoryTypeNames
        {
            get { return new[] { "Project" }; }
        }
        bool IUpdatingProvider.CanAppendIssueDescriptions
        {
            get { return true; }
        }
        bool IUpdatingProvider.CanChangeIssueStatuses
        {
            get { return true; }
        }
        bool IUpdatingProvider.CanCloseIssues
        {
            get { return false; }
        }

        public override string GetIssueUrl(IssueTrackerIssue issue)
        {
            if (issue == null)
                throw new ArgumentNullException("issue");

            var url = this.BaseUrl;
            if (!url.EndsWith("/"))
                url += "/";

            return url + "issue/" + Uri.EscapeUriString(issue.IssueId);
        }
        public IssueTrackerCategory[] GetCategories()
        {
            return new[] { new IssueTrackerCategory(AnyProjectCategory, AnyProjectCategory) }.Concat(this.session.Value.GetProjects()).ToArray();
        }
        public override IssueTrackerIssue[] GetIssues(string releaseNumber)
        {
            if (string.IsNullOrEmpty(releaseNumber))
                throw new ArgumentNullException("releaseNumber");

            return this.session.Value.GetIssues(this.CategoryIdFilter[0], releaseNumber, this.MaxIssues).ToArray();
        }
        public override bool IsIssueClosed(IssueTrackerIssue issue)
        {
            if (issue == null)
                throw new ArgumentNullException("issue");

            return ((YouTrackIssue)issue).IsResolved;
        }
        public override bool IsAvailable()
        {
            return true;
        }
        public override string ToString()
        {
            return "Issue tracking provider for JetBrains YouTrack.";
        }
        public override void ValidateConnection()
        {
            if (!string.IsNullOrEmpty(this.UserName))
                this.session.Value.Connect();
            else
                this.session.Value.GetProjects();
        }
        public void AppendIssueDescription(string issueId, string textToAppend)
        {
            if (string.IsNullOrEmpty(issueId))
                throw new ArgumentNullException("issueId");
            if (string.IsNullOrWhiteSpace(textToAppend))
                return;

            this.session.Value.ApplyCommandToIssue(issueId, "comment", textToAppend);
        }
        public void ChangeIssueStatus(string issueId, string newStatus)
        {
            if (string.IsNullOrEmpty(issueId))
                throw new ArgumentNullException("issueId");
            if (string.IsNullOrEmpty(newStatus))
                throw new ArgumentNullException("newStatus");

            this.session.Value.ApplyCommandToIssue(issueId, "state " + newStatus);
        }
        void IUpdatingProvider.CloseIssue(string issueId)
        {
            throw new NotSupportedException();
        }
    }
}
