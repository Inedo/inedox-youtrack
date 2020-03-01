using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Inedo.Documentation;
using Inedo.Extensibility.Credentials;
using Inedo.Extensibility.IssueSources;
using Inedo.Extensions.YouTrack.Credentials;
using Inedo.Extensions.YouTrack.SuggestionProviders;
using Inedo.Serialization;
using Inedo.Web;

namespace Inedo.Extensions.YouTrack.IssueSources
{
    [DisplayName("YouTrack Issue Source")]
    [Description("Issue source for JetBrains YouTrack.")]
    [PersistFrom("Inedo.BuildMasterExtensions.YouTrack.IssueSources.YouTrackIssueSource,YouTrack")]
    public sealed class YouTrackIssueSource : IssueSource<YouTrackSecureResource>, IMissingPersistentPropertyHandler
    {
        [Persistent]
        [Required]
        [DisplayName("Project name")]
        [SuggestableValue(typeof(YouTrackProjectSuggestionProvider))]
        public string ProjectName { get; set; }

        [Persistent]
        [DisplayName("Search query")]
        [PlaceholderText("Fix version: $ReleaseNumber")]
        public string Filter { get; set; }

        void IMissingPersistentPropertyHandler.OnDeserializedMissingProperties(IReadOnlyDictionary<string, string> missingProperties)
        {
            if (string.IsNullOrEmpty(this.ResourceName) && missingProperties.TryGetValue("CredentialName", out var credName))
                this.ResourceName = credName;
            if (missingProperties.TryGetValue("ReleaseNumber", out var relNum))
                this.Filter = $"{this.Filter} Fix version: {{{relNum}}}";

        }
        public override async Task<IEnumerable<IIssueTrackerIssue>> EnumerateIssuesAsync(IIssueSourceEnumerationContext context)
        {
            var filter = AH.CoalesceString(this.Filter, "Fix version: $ReleaseNumber");
            using (var client = new YouTrackClient(this.ResourceName, new CredentialResolutionContext(context.ProjectId, null)))
            {
                return await client.IssuesByProjectAsync(this.ProjectName, filter).ConfigureAwait(false);
            }
        }

        public override RichDescription GetDescription() => new RichDescription("YouTrack ", new Hilite(this.ProjectName), " in ", this.ResourceName);
    }
}
