﻿using System.ComponentModel;
using System.Threading.Tasks;
using Inedo.Diagnostics;
using Inedo.Documentation;
using Inedo.Extensibility;
using Inedo.Extensibility.Operations;
using Inedo.Web;

namespace Inedo.Extensions.YouTrack.Operations
{
    [DisplayName("Add Comment to YouTrack Issue")]
    [Description("Adds a comment to a YouTrack issue.")]
    [ScriptAlias("Add-Comment")]
    [ScriptNamespace("YouTrack")]
    [Tag("youtrack")]
    public sealed class AddYouTrackCommentOperation : YouTrackOperationBase
    {
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

            using (var client = this.CreateClient(context))
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
