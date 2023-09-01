#nullable enable

using System.ComponentModel;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using Inedo.Diagnostics;
using Inedo.Documentation;
using Inedo.Extensibility.Credentials;
using Inedo.Extensibility.IssueTrackers;
using Inedo.Serialization;
using Inedo.Web;

namespace Inedo.Extensions.YouTrack.Credentials;

[DisplayName("YouTrack Account")]
[Description("Provides secure access to YouTrack.")]
public sealed class YouTrackServiceCredentials : ServiceCredentials, IIssueTrackerServiceCredentials
{
    [Persistent]
    [DisplayName("Username")]
    public string? UserName { get; set; }

    [Persistent(Encrypted = true)]
    [DisplayName("Permanent token")]
    public SecureString? PermanentToken { get; set; }

    SecureString? IIssueTrackerServiceCredentials.Password { get => this.PermanentToken; set => this.PermanentToken = value; }

    public override RichDescription GetCredentialDescription() => new($"{this.UserName} (token)");

    public override RichDescription GetServiceDescription()
    {
        return this.TryGetServiceUrlHostName(out var hostName)
            ? new("YouTrack (", new Hilite(hostName), ")")
            : new("YouTrack");
    }

    public override ValueTask<ValidationResults> ValidateAsync(CancellationToken cancellationToken = default)
    {
        var service = new YouTrackServiceInfo();
        service.MessageLogged += (_, args) => this.Log(args.Level, args.Message, args.Details, args.Category, args.ContextData, args.Exception);
        return service.ValidateCredentialsAsync(this, cancellationToken);
    }
}