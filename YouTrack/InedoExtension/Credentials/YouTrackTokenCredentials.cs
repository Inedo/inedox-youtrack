using System.ComponentModel;
using System.Security;
using Inedo.Documentation;
using Inedo.Extensibility.Credentials;
using Inedo.Serialization;

namespace Inedo.Extensions.YouTrack.Credentials;

[DisplayName("[Legacy] YouTrack Permanent Token")]
[Description("Used in BuildMaster 2022 and earlier to provide access to a YouTrack instance.")]
public sealed class YouTrackTokenCredentials : SecureCredentials
{
    [Persistent(Encrypted = true)]
    [DisplayName("Permanent token")]
    public SecureString PermanentToken { get; set; }

    public override RichDescription GetDescription() => new("[Legacy] (secret)");
}
