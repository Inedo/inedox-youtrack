using Inedo.BuildMaster.Extensibility.Credentials;
using Inedo.BuildMaster.Extensibility.IssueSources;
using Inedo.BuildMaster.Extensibility.IssueTrackerConnections;
using Inedo.Documentation;
using Inedo.Extensions.YouTrack;
using Inedo.Extensions.YouTrack.Credentials;
using Inedo.Serialization;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Inedo.BuildMasterExtensions.YouTrack.IssueSources
{
    public sealed class YouTrackIssueSource : IssueSource, IHasCredentials<YouTrackCredentials>
    {
        [Persistent]
        [Required]
        [DisplayName("Credentials")]
        public string CredentialName { get; set; }

        [Persistent]
        [Required]
        [DisplayName("Project name")]
        public string ProjectName { get; set; }

        [Persistent]
        [DisplayName("Search query")]
        public string Filter { get; set; }

        public override async Task<IEnumerable<IIssueTrackerIssue>> EnumerateIssuesAsync(IIssueSourceEnumerationContext context)
        {
            var credentials = this.TryGetCredentials();

            using (var client = new YouTrackClient(credentials))
            {
                return await client.IssuesByProjectAsync(this.ProjectName, this.Filter).ConfigureAwait(false);
            }
        }

        public override RichDescription GetDescription()
        {
            var credentials = this.TryGetCredentials();
            return new RichDescription("YouTrack ", new Hilite(this.ProjectName), " in ", new Hilite(credentials?.ToString() ?? "[missing credentials]"));
        }
    }
}
