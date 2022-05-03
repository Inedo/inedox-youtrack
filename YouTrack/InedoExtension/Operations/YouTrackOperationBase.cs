using System;
using System.ComponentModel;
using System.Security;
using System.Threading.Tasks;
using Inedo.Diagnostics;
using Inedo.Documentation;
using Inedo.Extensibility;
using Inedo.Extensibility.Credentials;
using Inedo.Extensibility.Operations;
using Inedo.Extensibility.SecureResources;
using Inedo.Extensions.YouTrack.Credentials;
using Inedo.Web;

namespace Inedo.Extensions.YouTrack.Operations
{
    [Tag("youtrack")]
    public abstract class YouTrackOperationBase : ExecuteOperation
    {
        protected YouTrackOperationBase()
        {
        }

        [DisplayName("From resource")]
        [ScriptAlias("From")]
        [ScriptAlias("Credentials")]
        [SuggestableValue(typeof(SecureResourceSuggestionProvider<YouTrackSecureResource>))]
        public string ResourceName { get; set; }

        [Category("Connection")]
        [DisplayName("YouTrack server URL")]
        [ScriptAlias("Server")]
        [PlaceholderText("Use server URL from resource")]
        public string ServerUrl { get; set; }

        [Category("Connection")]
        [DisplayName("Credentials")]
        [ScriptAlias("CredentialName")]
        [PlaceholderText("Use credential from resource")]
        public string CredentialName { get; set; }

        [Category("Connection")]
        [DisplayName("Permanent token")]
        [ScriptAlias("PermanentToken")]
        [PlaceholderText("Use permanent token from credential")]
        public SecureString PermanentToken { get; set; }

        public sealed override async Task ExecuteAsync(IOperationExecutionContext context)
        {
            try
            {
                await this.ExecuteAsync(context, this.GetClient(context));
            }
            catch (YouTrackException ex)
            {
                this.LogError(ex.Message);
            }
        }

        private protected abstract Task ExecuteAsync(IOperationExecutionContext context, YouTrackClient client);

        private YouTrackClient GetClient(IOperationExecutionContext context)
        {
            var resourceName = this.ResourceName;
            var credentialContext = (ICredentialResolutionContext)context;

            YouTrackSecureResource resource = null;
            SecureCredentials credentials = null;
            if (!string.IsNullOrEmpty(resourceName))
            {
                resource = SecureResource.TryCreate(resourceName, credentialContext) as YouTrackSecureResource;
                credentials = resource?.GetCredentials(credentialContext);
                if (resource == null)
                    throw new InvalidOperationException($"The resource \"{resourceName}\" was not found.");
            }

            if (!string.IsNullOrEmpty(this.CredentialName))
                credentials = SecureCredentials.Create(this.CredentialName, credentialContext);

            var serverUrl = AH.CoalesceString(this.ServerUrl, resource?.ServerUrl);
            if (string.IsNullOrEmpty(serverUrl))
                throw new InvalidOperationException("ServerUrl is not set.");

            var token = AH.Unprotect(this.PermanentToken?.Length > 0 ? this.PermanentToken : (credentials as YouTrackTokenCredentials)?.PermanentToken);
            return new YouTrackClient(token, serverUrl, this);
        }
    }
}
