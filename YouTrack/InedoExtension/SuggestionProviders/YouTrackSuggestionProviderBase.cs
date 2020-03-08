using System.Collections.Generic;
using System.Threading.Tasks;
using Inedo.Extensibility;
using Inedo.Extensibility.Credentials;
using Inedo.Extensions.YouTrack.IssueSources;
using Inedo.Web;

namespace Inedo.Extensions.YouTrack.SuggestionProviders
{
    public abstract class YouTrackSuggestionProviderBase : ISuggestionProvider
    {
        public abstract Task<IEnumerable<string>> GetSuggestionsAsync(IComponentConfiguration config);

        internal YouTrackClient CreateClient(IComponentConfiguration config)
        {
            var resourceName = config[nameof(YouTrackIssueSource.ResourceName)];
            if (string.IsNullOrEmpty(resourceName))
                return null;

            return new YouTrackClient(resourceName, config.EditorContext as ICredentialResolutionContext ?? CredentialResolutionContext.None);
        }
    }
}
