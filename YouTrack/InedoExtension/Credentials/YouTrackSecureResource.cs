using System.ComponentModel;
using Inedo.Documentation;
using Inedo.Extensibility.SecureResources;
using Inedo.Serialization;

namespace Inedo.Extensions.YouTrack.Credentials
{
    [DisplayName("YouTrack Instance")]
    [Description("Connect to an instance of YouTrack.")]
    public sealed class YouTrackSecureResource : SecureResource<YouTrackTokenCredentials>
    {
        [Required]
        [Persistent]
        [DisplayName("YouTrack server URL")]
        [PlaceholderText("https://example.myjetbrains.com/youtrack/")]
        public string ServerUrl { get; set; }

        public override RichDescription GetDescription() => new(this.ServerUrl);
    }
}
