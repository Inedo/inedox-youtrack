using System.ComponentModel;
using System.Threading.Tasks;
using Inedo.Documentation;
using Inedo.Extensibility;
using Inedo.Extensibility.Operations;

namespace Inedo.Extensions.YouTrack.Operations
{
    [Tag("youtrack")]
    [ScriptNamespace("YouTrack")]
    [ScriptAlias("Release-Version")]
    [DefaultProperty(nameof(Version))]
    [DisplayName("Release YouTrack Version")]
    [Description("Marks a version in YouTrack as released or archived.")]
    public sealed class ReleaseYouTrackVersionOperation : YouTrackOperationBase
    {
        [Required]
        [ScriptAlias("Project")]
        [DefaultValue("$ApplicationName")]
        public string Project { get; set; } = "$ApplicationName";
        [Required]
        [ScriptAlias("Version")]
        [DefaultValue("$ReleaseNumber")]
        public string Version { get; set; } = "$ReleaseNumber";
        [DefaultValue(true)]
        [ScriptAlias("Release")]
        public bool Release { get; set; } = true;
        [DefaultValue(true)]
        [ScriptAlias("Archive")]
        public bool Archive { get; set; } = true;

        public override async Task ExecuteAsync(IOperationExecutionContext context)
        {
            using var client = this.CreateClient(context);
            await client.ReleaseVersionAsync(this.Project, this.Version, this.Release, this.Archive, context.CancellationToken);
        }

        protected override ExtendedRichDescription GetDescription(IOperationConfiguration config)
        {
            return new ExtendedRichDescription(
                new RichDescription(
                    "Release YouTrack version ",
                    new Hilite(config[nameof(Version)]),
                    " in ",
                    new Hilite(config[nameof(Project)])
                )
            );
        }
    }
}
