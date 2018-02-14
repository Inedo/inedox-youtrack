using System.Collections.Generic;
using System.Threading.Tasks;
using Inedo.Extensibility;
using Inedo.Extensibility.Credentials;
using Inedo.Extensions.YouTrack.Credentials;
using Inedo.Extensions.YouTrack.Operations;
using Inedo.Web;

namespace Inedo.Extensions.YouTrack.SuggestionProviders
{
    public abstract class YouTrackSuggestionProviderBase : ISuggestionProvider
    {
        public abstract Task<IEnumerable<string>> GetSuggestionsAsync(IComponentConfiguration config);

        protected YouTrackClient CreateClient(IComponentConfiguration config)
        {
            var credentials = ResourceCredentials.Create<YouTrackCredentials>(config[nameof(IHasCredentials<YouTrackCredentials>.CredentialName)]);
            return new YouTrackClient(
                new YouTrackCredentials
                {
                    ServerUrl = AH.CoalesceString(config[nameof(YouTrackOperationBase.ServerUrl)], credentials?.ServerUrl),
                    UserName = AH.CoalesceString(config[nameof(YouTrackOperationBase.UserName)], credentials?.UserName),
                    Password = AH.CreateSecureString(AH.CoalesceString(config[nameof(YouTrackOperationBase.Password)], AH.Unprotect(credentials?.Password))),
                    PermanentToken = AH.CreateSecureString(AH.CoalesceString(config[nameof(YouTrackOperationBase.PermanentToken)], AH.Unprotect(credentials?.PermanentToken))),
                }
            );
        }
    }
}
