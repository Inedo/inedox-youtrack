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
    [DisplayName("Add Comment to YouTrack Issue")]
    [ScriptAlias("Add-Comment")]
    [ScriptNamespace("YouTrack")]
    public sealed class AddYouTrackCommentOperation : YouTrackOperationBase
    {
        [DisplayName("Credentials")]
        [ScriptAlias("Credentials")]
        public override string CredentialName { get; set; }

        [Required]
        [DisplayName("Issue ID")]
        [ScriptAlias("IssueId")]
        public string IssueId { get; set; }

        [Required]
        [DisplayName("Comment")]
        [ScriptAlias("Comment")]
        [FieldEditMode(FieldEditMode.Multiline)]
        public string Comment { get; set; }

        public override async Task ExecuteAsync(IOperationExecutionContext context)
        {
            if (context.Simulation)
            {
                this.LogDebug("Simulating; not creating a comment.");
                return;
            }

            using (var client = this.CreateClient())
            {
                await client.RunCommandAsync(this.IssueId, "comment", this.Comment, context.CancellationToken).ConfigureAwait(false);
            }
        }

        protected override ExtendedRichDescription GetDescription(IOperationConfiguration config)
        {
            return new ExtendedRichDescription(new RichDescription(
                "Add comment to YouTrack issue ", new Hilite(config[nameof(IssueId)])
            ), new RichDescription(config[nameof(Comment)]));
        }
    }
}
