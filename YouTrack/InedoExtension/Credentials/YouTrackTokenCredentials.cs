using System.ComponentModel;
using System.Security;
using Inedo.Documentation;
using Inedo.Extensibility.Credentials;
using Inedo.Serialization;

namespace Inedo.Extensions.YouTrack.Credentials
{
    [DisplayName("YouTrack Permanent Token")]
    [Description("Provides secure access to YouTrack.")]
    public sealed class YouTrackTokenCredentials : SecureCredentials
    {
        [Persistent(Encrypted = true)]
        [DisplayName("Permanent token")]
        public SecureString PermanentToken { get; set; }

        public override RichDescription GetDescription() => new RichDescription("(secret)");
    }
}
