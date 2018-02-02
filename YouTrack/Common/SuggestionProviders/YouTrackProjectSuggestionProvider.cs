using System.Collections.Generic;
using System.Threading.Tasks;
using Inedo.Extensibility;

namespace Inedo.Extensions.YouTrack.SuggestionProviders
{
    public sealed class YouTrackProjectSuggestionProvider : YouTrackSuggestionProviderBase
    {
        public override async Task<IEnumerable<string>> GetSuggestionsAsync(IComponentConfiguration config)
        {
            using (var client = this.CreateClient(config))
            {
                return await client.ListProjectsAsync().ConfigureAwait(false);
            }
        }
    }
}
