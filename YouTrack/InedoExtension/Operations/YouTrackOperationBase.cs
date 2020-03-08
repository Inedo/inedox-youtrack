using System.ComponentModel;
using System.Security;
using Inedo.Documentation;
using Inedo.Extensibility;
using Inedo.Extensibility.Credentials;
using Inedo.Extensibility.Operations;
using Inedo.Extensions.YouTrack.Credentials;
using Inedo.Web;

namespace Inedo.Extensions.YouTrack.Operations
{
    public abstract class YouTrackOperationBase : ExecuteOperation
    {
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
        [DisplayName("User name")]
        [ScriptAlias("UserName")]
        [PlaceholderText("Use user name from credential")]
        public string UserName { get; set; }

        [Category("Connection")]
        [DisplayName("Password")]
        [ScriptAlias("Password")]
        [PlaceholderText("Use password from credential")]
        public SecureString Password { get; set; }

        [Category("Connection")]
        [DisplayName("Permanent token")]
        [ScriptAlias("PermanentToken")]
        [PlaceholderText("Use permanent token from credential")]
        public SecureString PermanentToken { get; set; }

        internal YouTrackClient CreateClient(IOperationExecutionContext context) => new YouTrackClient(this, (ICredentialResolutionContext)context);
    }
}
