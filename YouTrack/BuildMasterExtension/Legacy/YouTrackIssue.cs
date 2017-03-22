using Inedo.BuildMaster.Extensibility.Providers.IssueTracking;
using System;

namespace Inedo.BuildMasterExtensions.YouTrack.Legacy
{
    [Serializable]
    internal sealed class YouTrackIssue : IssueTrackerIssue
    {
        public YouTrackIssue(string issueId, string issueStatus, string issueTitle, string issueDescription, string releaseNumber, bool isResolved)
            : base(issueId, issueStatus, issueTitle, issueDescription, releaseNumber)
        {
            this.IsResolved = isResolved;
        }

        public bool IsResolved { get; private set; }
    }
}
