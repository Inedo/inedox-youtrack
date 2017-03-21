#if BuildMaster
using Inedo.BuildMaster.Extensibility;
using Inedo.BuildMaster.Extensibility.Credentials;
using Inedo.BuildMaster.Web;
#elif Otter
using Inedo.Otter.Extensibility;
using Inedo.Otter.Extensibility.Credentials;
using Inedo.Otter.Extensions;
#endif
using Inedo.Documentation;
using Inedo.Serialization;
using System.ComponentModel;
using System.Security;

namespace Inedo.Extensions.YouTrack.Credentials
{
    [ScriptAlias("YouTrack")]
    [DisplayName("YouTrack")]
    [Description("Credentials for JetBrains YouTrack.")]
    public sealed class YouTrackCredentials : ResourceCredentials
    {
        [Required]
        [Persistent]
        [DisplayName("YouTrack server URL")]
        [PlaceholderText("https://example.myjetbrains.com/youtrack/")]
        public string ServerUrl { get; set; }

        [Persistent]
        [DisplayName("User name")]
        [PlaceholderText("Anonymous")]
        public string UserName { get; set; }

        [Persistent(Encrypted = true)]
        [DisplayName("Password")]
        [FieldEditMode(FieldEditMode.Password)]
        public SecureString Password { get; set; }

        [Persistent(Encrypted = true)]
        [DisplayName("Permanent token")]
        [Description("Overrides user name and password if specified.")]
        [FieldEditMode(FieldEditMode.Password)]
        public SecureString PermanentToken { get; set; }

        public override RichDescription GetDescription()
        {
            if (this.PermanentToken?.Length > 0)
            {
                return new RichDescription("[token] @ ", this.ServerUrl);
            }
            if (!string.IsNullOrEmpty(this.UserName))
            {
                return new RichDescription(this.UserName, " @ ", this.ServerUrl);
            }
            return new RichDescription("[Anonymous] @ ", this.ServerUrl);
        }
    }
}
