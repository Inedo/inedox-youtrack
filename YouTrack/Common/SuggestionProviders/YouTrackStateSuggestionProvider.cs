using System.Collections.Generic;
using System.Threading.Tasks;
using Inedo.Extensibility;

namespace Inedo.Extensions.YouTrack.SuggestionProviders
{
    public sealed class YouTrackStateSuggestionProvider : YouTrackSuggestionProviderBase
    {
        public override async Task<IEnumerable<string>> GetSuggestionsAsync(IComponentConfiguration config)
        {
            using (var client = this.CreateClient(config))
            {
                return await client.ListStatesAsync().ConfigureAwait(false);
            }
        }
    }
}
