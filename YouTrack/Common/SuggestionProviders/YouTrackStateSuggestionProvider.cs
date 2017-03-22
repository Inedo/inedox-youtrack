#if BuildMaster
using Inedo.BuildMaster.Extensibility;
#elif Otter
using Inedo.Otter.Extensibility;
#endif
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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
