using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Inedo.Documentation;
using Inedo.Extensibility.Credentials;
using Inedo.Extensibility.IssueSources;
using Inedo.Extensibility.SecureResources;
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
        [Required]
        [Persistent]
        [DisplayName("Project name")]
        [SuggestableValue(typeof(YouTrackProjectSuggestionProvider))]
        public string ProjectName { get; set; }
        [Required]
        [Persistent]
        [DisplayName("Search query")]
        public string Filter { get; set; } = "Fix version: $ReleaseNumber";

        [Persistent]
        [Category("Advanced")]
        [DefaultValue("$YouTrackStatusFieldName")]
        [DisplayName("Issue status custom field")]
        public string IssueStatusFieldName { get; set; } = "$YouTrackStatusFieldName";
        [Persistent]
        [Category("Advanced")]
        [DefaultValue("$YouTrackTypeFieldName")]
        [DisplayName("Issue type custom field")]
        public string IssueTypeFieldName { get; set; } = "$YouTrackTypeFieldName";

        public override async Task<IEnumerable<IIssueTrackerIssue>> EnumerateIssuesAsync(IIssueSourceEnumerationContext context)
        {
            var client = this.GetClient(context);
            return await client.GetIssuesAsync(this.ProjectName, this.Filter, this.IssueStatusFieldName, this.IssueTypeFieldName).ConfigureAwait(false);
        }
        public override RichDescription GetDescription() => new("YouTrack ", new Hilite(this.ProjectName), " in ", this.ResourceName);

        private YouTrackClient GetClient(IIssueSourceEnumerationContext context)
        {
            var resourceName = this.ResourceName;
            var credentialContext = new CredentialResolutionContext(context.ProjectId, null);

            string token = null;
            YouTrackSecureResource resource = null;
            if (!string.IsNullOrEmpty(resourceName))
            {
                resource = SecureResource.TryCreate(resourceName, credentialContext) as YouTrackSecureResource;
                var credentials = resource?.GetCredentials(credentialContext);
                if (resource == null)
                {
                    var resCred = ResourceCredentials.TryCreate<LegacyYouTrackResourceCredentials>(resourceName);
                    resource = (YouTrackSecureResource)resCred?.ToSecureResource();
                    credentials = resCred?.ToSecureCredentials();
                }

                if (resource == null)
                    throw new InvalidOperationException($"The resource \"{resourceName}\" was not found.");

                if (credentials is not YouTrackTokenCredentials tokenCredentials)
                    throw new InvalidOperationException($"The resource \"{resourceName}\" does not refer to YouTrack token credentials.");

                token = AH.Unprotect(tokenCredentials.PermanentToken);
            }

            return new YouTrackClient(token, resource.ServerUrl, context.Log);
        }

        void IMissingPersistentPropertyHandler.OnDeserializedMissingProperties(IReadOnlyDictionary<string, string> missingProperties)
        {
            if (string.IsNullOrEmpty(this.ResourceName) && missingProperties.TryGetValue("CredentialName", out var credName))
                this.ResourceName = credName;
            if (missingProperties.TryGetValue("ReleaseNumber", out var r))
                this.Filter = $"{this.Filter} Fix version: {r}";
        }
    }
}
