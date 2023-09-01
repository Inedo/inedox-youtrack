using Inedo.Documentation;
using System.ComponentModel;
using Inedo.Extensibility.SecureResources;
using Inedo.Serialization;

namespace Inedo.Extensions.YouTrack.Credentials;

[DisplayName("[Legacy] YouTrack Instance")]
[Description("Used in BuildMaster 2022 and earlier to connect to a YouTrack instance and work with issues in projects.")]
[Undisclosed]
public sealed class YouTrackSecureResource : SecureResource<YouTrackTokenCredentials>
{
    [Required]
    [Persistent]
    [DisplayName("YouTrack server URL")]
    [PlaceholderText("https://example.myjetbrains.com/youtrack/")]
    public string ServerUrl { get; set; }

    public override RichDescription GetDescription() => new($"[Legacy] {this.ServerUrl}");
}
