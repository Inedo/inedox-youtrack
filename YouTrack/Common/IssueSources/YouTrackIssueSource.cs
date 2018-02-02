using Inedo.BuildMaster.Extensibility;
using Inedo.BuildMaster.Extensibility.Credentials;
using Inedo.BuildMaster.Extensibility.IssueSources;
using Inedo.BuildMaster.Extensibility.IssueTrackerConnections;
using Inedo.BuildMaster.Web.Controls;
using Inedo.Documentation;
using Inedo.Extensions.YouTrack;
using Inedo.Extensions.YouTrack.Credentials;
using Inedo.Extensions.YouTrack.SuggestionProviders;
using Inedo.Serialization;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Inedo.BuildMasterExtensions.YouTrack.IssueSources
{
    [DisplayName("YouTrack Issue Source")]
    [Description("Issue source for JetBrains YouTrack.")]
    public sealed class YouTrackIssueSource : IssueSource, IHasCredentials<YouTrackCredentials>
    {
        [Persistent]
        [Required]
        [DisplayName("Credentials")]
        public string CredentialName { get; set; }

        [Persistent]
        [Required]
        [DisplayName("Project name")]
        [SuggestibleValue(typeof(YouTrackProjectSuggestionProvider))]
        public string ProjectName { get; set; }

        [Persistent]
        [DisplayName("Fix version")]
        public string ReleaseNumber { get; set; }

        [Persistent]
        [DisplayName("Search query")]
        public string Filter { get; set; }

        public override async Task<IEnumerable<IIssueTrackerIssue>> EnumerateIssuesAsync(IIssueSourceEnumerationContext context)
        {
            var credentials = this.TryGetCredentials();

            var filter = this.Filter ?? string.Empty;
            if (!string.IsNullOrEmpty(this.ReleaseNumber))
            {
                filter = $"{filter} Fix version: {{{this.ReleaseNumber}}}";
            }

            using (var client = new YouTrackClient(credentials))
            {
                return await client.IssuesByProjectAsync(this.ProjectName, filter).ConfigureAwait(false);
            }
        }

        public override RichDescription GetDescription()
        {
            var credentials = this.TryGetCredentials();
            return new RichDescription("YouTrack ", new Hilite(this.ProjectName), " in ", credentials.GetDescription());
        }
    }
}
