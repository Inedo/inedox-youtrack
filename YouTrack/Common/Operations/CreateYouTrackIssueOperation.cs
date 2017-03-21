#if BuildMaster
using Inedo.BuildMaster.Extensibility;
using Inedo.BuildMaster.Extensibility.Operations;
using Inedo.BuildMaster.Web;
#elif Otter
using Inedo.Otter.Extensibility;
using Inedo.Otter.Extensibility.Operations;
using Inedo.Otter.Extensions;
#endif
using Inedo.Diagnostics;
using Inedo.Documentation;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Inedo.Extensions.YouTrack.Operations
{
    [DisplayName("Create YouTrack Issue")]
    [ScriptAlias("Create-Issue")]
    [ScriptNamespace("YouTrack")]
    public sealed class CreateYouTrackIssueOperation : YouTrackOperationBase
    {
        [DisplayName("Credentials")]
        [ScriptAlias("Credentials")]
        public override string CredentialName { get; set; }

        [Required]
        [DisplayName("Project")]
        [ScriptAlias("Project")]
        public string Project { get; set; }

        [Required]
        [DisplayName("Summary")]
        [ScriptAlias("Summary")]
        public string Summary { get; set; }

        [DisplayName("Description")]
        [ScriptAlias("Description")]
        [FieldEditMode(FieldEditMode.Multiline)]
        public string Description { get; set; }

        [Output]
        [DisplayName("Issue ID")]
        [ScriptAlias("IssueId")]
        public string IssueId { get; set; }

        public override async Task ExecuteAsync(IOperationExecutionContext context)
        {
            if (context.Simulation)
            {
                this.LogDebug("Simulating; not creating an issue.");
                this.IssueId = "ABC-123";
                return;
            }

            using (var client = this.CreateClient())
            {
                this.IssueId = await client.CreateIssueAsync(this.Project, this.Summary, this.Description, context.CancellationToken).ConfigureAwait(false);
            }
        }

        protected override ExtendedRichDescription GetDescription(IOperationConfiguration config)
        {
            return new ExtendedRichDescription(new RichDescription(
                "Create YouTrack issue in ", new Hilite(config[nameof(Project)]),
                ": ", new Hilite(config[nameof(Summary)])
            ));
        }
    }
}
