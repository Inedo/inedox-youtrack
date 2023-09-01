using System.ComponentModel;
using System.Text;
using Inedo.Diagnostics;
using Inedo.Documentation;
using Inedo.Extensibility;
using System.Threading.Tasks;
using Inedo.Extensibility.Operations;

namespace Inedo.Extensions.YouTrack.Operations;

[ScriptNamespace("YouTrack")]
[ScriptAlias("Ensure-Version")]
[ScriptAlias("Release-Version", Obsolete = true)]
[DefaultProperty(nameof(Version))]
[Description("Ensures that a version exists for a YouTrack project and optionally updates its released and archived states.")]
[Example(@"# Ensure that a version exists in YouTrack for the current release of the current application.
YouTrack::Ensure-Version
(
    Project: $ApplicationName,
    Version: $ReleaseNumber
);")]
[Example(@"# Ensure that a version exists and has been released and archived in YouTrack for the current release of the current application.
YouTrack::Ensure-Version
(
    Project: $ApplicationName,
    Version: $ReleaseNumber,
    Released: true,
    Archived: true
);")]
public sealed class EnsureYouTrackVersionOperation : YouTrackOperationBase
{
    [Required]
    [ScriptAlias("Project")]
    [DefaultValue("$ApplicationName")]
    public string Project { get; set; } = "$ApplicationName";
    [Required]
    [ScriptAlias("Version")]
    [DefaultValue("$ReleaseNumber")]
    public string Version { get; set; } = "$ReleaseNumber";
    [ScriptAlias("Released")]
    [ScriptAlias("Release", Obsolete = true)]
    [PlaceholderText("no change")]
    public bool? Released { get; set; }
    [ScriptAlias("Archived")]
    [ScriptAlias("Archive", Obsolete = true)]
    [PlaceholderText("no change")]
    public bool? Archived { get; set; }
    [Category("Advanced")]
    [ScriptAlias("VersionFieldName")]
    [DisplayName("Version custom field")]
    [DefaultValue("$YouTrackVersionFieldName")]
    public string VersionFieldName { get; set; } = "$YouTrackVersionFieldName";

    private protected override async Task ExecuteAsync(IOperationExecutionContext context, YouTrackClient client)
    {
        this.Project = AH.CoalesceString(this.Project, client.DefaultProjectName);

        var message = new StringBuilder($"Ensuring \"{this.VersionFieldName}\" for project {this.Project} has a version {this.Version}");
        if (this.Released.HasValue || this.Archived.HasValue)
        {
            message.Append(" that is ");
            if (this.Released.HasValue)
                message.Append(this.Released.GetValueOrDefault() ? "released" : "not released");

            if (this.Archived.HasValue)
            {
                if (this.Released.HasValue)
                    message.Append(" and ");

                message.Append(this.Archived.GetValueOrDefault() ? "archived" : "not archived");
            }
        }

        message.Append("...");
        this.LogInformation(message.ToString());

        if (context.Simulation)
        {
            this.LogDebug("Simulating; not creating or updating version.");
            return;
        }

        await client.EnsureVersionAsync(this.VersionFieldName, this.Project, this.Version, this.Released, this.Archived, context.CancellationToken).ConfigureAwait(false);
    }

    protected override ExtendedRichDescription GetDescription(IOperationConfiguration config)
    {
        bool setReleased = bool.TryParse(config[nameof(Released)], out bool released);
        bool setArchived = bool.TryParse(config[nameof(Archived)], out bool archived);

        var details = new RichDescription();
        if (setReleased)
            details.AppendContent("set released to " + released.ToString().ToLowerInvariant());
        if (setReleased && setArchived)
            details.AppendContent(" and ");
        if (setArchived)
            details.AppendContent("set archived to " + archived.ToString().ToLowerInvariant());

        return new ExtendedRichDescription(
            new RichDescription(
                "Ensure YouTrack version ",
                new Hilite(config[nameof(Version)]),
                " exists in ",
                new Hilite(AH.CoalesceString(config[nameof(Project)], config[nameof(ResourceName)]))
            )
        );
    }
}
