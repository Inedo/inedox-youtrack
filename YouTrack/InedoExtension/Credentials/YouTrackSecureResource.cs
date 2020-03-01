using System.ComponentModel;
using Inedo.Documentation;
using Inedo.Extensibility.SecureResources;
using Inedo.Serialization;
using UsernamePasswordCredentials = Inedo.Extensions.Credentials.UsernamePasswordCredentials;

namespace Inedo.Extensions.YouTrack.Credentials
{
    public sealed class YouTrackSecureResource : SecureResource<YouTrackTokenCredentials, UsernamePasswordCredentials>
    {
        [Required]
        [Persistent]
        [DisplayName("YouTrack server URL")]
        [PlaceholderText("https://example.myjetbrains.com/youtrack/")]
        public string ServerUrl { get; set; }

        public override RichDescription GetDescription() => new RichDescription(this.ServerUrl);
    }
}
