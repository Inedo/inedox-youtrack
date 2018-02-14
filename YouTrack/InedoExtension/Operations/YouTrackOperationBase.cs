using Inedo.Documentation;
using Inedo.Extensibility;
using Inedo.Extensibility.Credentials;
using Inedo.Extensibility.Operations;
using Inedo.Extensions.YouTrack.Credentials;
using System.ComponentModel;
using System.Security;

namespace Inedo.Extensions.YouTrack.Operations
{
    public abstract class YouTrackOperationBase : ExecuteOperation, IHasCredentials<YouTrackCredentials>
    {
        [DisplayName("Credentials")]
        [ScriptAlias("Credentials")]
        public abstract string CredentialName { get; set; }

        [Category("Connection")]
        [DisplayName("YouTrack server URL")]
        [ScriptAlias("Server")]
        [PlaceholderText("Use server URL from credential")]
        [MappedCredential(nameof(YouTrackCredentials.ServerUrl))]
        public string ServerUrl { get; set; }

        [Category("Connection")]
        [DisplayName("User name")]
        [ScriptAlias("UserName")]
        [PlaceholderText("Use user name from credential")]
        [MappedCredential(nameof(YouTrackCredentials.UserName))]
        public string UserName { get; set; }

        [Category("Connection")]
        [DisplayName("Password")]
        [ScriptAlias("Password")]
        [PlaceholderText("Use password from credential")]
        [MappedCredential(nameof(YouTrackCredentials.Password))]
        public SecureString Password { get; set; }

        [Category("Connection")]
        [DisplayName("Permanent token")]
        [ScriptAlias("PermanentToken")]
        [PlaceholderText("Use permanent token from credential")]
        [MappedCredential(nameof(YouTrackCredentials.PermanentToken))]
        public SecureString PermanentToken { get; set; }

        protected YouTrackClient CreateClient()
        {
            return new YouTrackClient(new YouTrackCredentials()
            {
                ServerUrl = this.ServerUrl,
                UserName = this.UserName,
                Password = this.Password,
                PermanentToken = this.PermanentToken
            });
        }
    }
}
