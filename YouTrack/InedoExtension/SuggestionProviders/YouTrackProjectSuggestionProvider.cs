using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Inedo.Extensibility;
using Inedo.Extensibility.Credentials;
using Inedo.Extensibility.SecureResources;
using Inedo.Extensions.YouTrack.Credentials;
using Inedo.Extensions.YouTrack.IssueSources;
using Inedo.Web;

namespace Inedo.Extensions.YouTrack.SuggestionProviders
{
    public sealed class YouTrackProjectSuggestionProvider : ISuggestionProvider
    {
        public async Task<IEnumerable<string>> GetSuggestionsAsync(IComponentConfiguration config)
        {
            var names = new List<string>();

            var client = CreateClient(config);
            await foreach (var p in client.GetProjectsAsync().ConfigureAwait(false))
                names.Add(p.Name);

            names.Sort();
            return names;
        }

        private static YouTrackClient CreateClient(IComponentConfiguration config)
        {
            var resourceName = config[nameof(YouTrackIssueSource.ResourceName)];
            if (string.IsNullOrEmpty(resourceName))
                return null;

            var credentialContext = config.EditorContext as ICredentialResolutionContext ?? CredentialResolutionContext.None;
            string token = null;
            YouTrackSecureResource resource = null;
            if (!string.IsNullOrEmpty(resourceName))
            {
                resource = SecureResource.TryCreate(resourceName, credentialContext) as YouTrackSecureResource;
                var credentials = resource?.GetCredentials(credentialContext);
                if (resource == null)
                    throw new InvalidOperationException($"The resource \"{resourceName}\" was not found.");

                if (credentials is not YouTrackTokenCredentials tokenCredentials)
                    throw new InvalidOperationException($"The resource \"{resourceName}\" does not refer to YouTrack token credentials.");

                token = AH.Unprotect(tokenCredentials.PermanentToken);
            }

            return new YouTrackClient(token, resource.ServerUrl);
        }
    }
}
