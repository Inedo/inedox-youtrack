#if BuildMaster
using Inedo.BuildMaster.Extensibility;
using Inedo.BuildMaster.Extensibility.Credentials;
using Inedo.BuildMaster.Web.Controls;
#elif Otter
using Inedo.Otter.Extensibility;
using Inedo.Otter.Extensibility.Credentials;
using Inedo.Otter.Web.Controls;
#endif
using Inedo.Extensions.YouTrack.Credentials;
using Inedo.Extensions.YouTrack.Operations;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Inedo.Extensions.YouTrack.SuggestionProviders
{
    public abstract class YouTrackSuggestionProviderBase : ISuggestionProvider
    {
        public abstract Task<IEnumerable<string>> GetSuggestionsAsync(IComponentConfiguration config);

        protected YouTrackClient CreateClient(IComponentConfiguration config)
        {
            var credentials = ResourceCredentials.Create<YouTrackCredentials>(config[nameof(IHasCredentials<YouTrackCredentials>.CredentialName)]);
            return new YouTrackClient(new YouTrackCredentials()
            {
                ServerUrl = AH.CoalesceString(config[nameof(YouTrackOperationBase.ServerUrl)], credentials?.ServerUrl),
                UserName = AH.CoalesceString(config[nameof(YouTrackOperationBase.UserName)], credentials?.UserName),
                Password = AH.CoalesceString(config[nameof(YouTrackOperationBase.Password)], credentials?.Password).ToSecureString(),
                PermanentToken = AH.CoalesceString(config[nameof(YouTrackOperationBase.PermanentToken)], credentials?.PermanentToken).ToSecureString(),
            });
        }
    }
}
