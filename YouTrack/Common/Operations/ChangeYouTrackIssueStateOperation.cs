#if BuildMaster
using Inedo.BuildMaster.Extensibility;
using Inedo.BuildMaster.Extensibility.Operations;
using Inedo.BuildMaster.Web.Controls;
#elif Otter
using Inedo.Otter.Extensibility;
using Inedo.Otter.Extensibility.Operations;
using Inedo.Otter.Web.Controls;
#endif
using Inedo.Diagnostics;
using Inedo.Documentation;
using Inedo.Extensions.YouTrack.SuggestionProviders;
using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace Inedo.Extensions.YouTrack.Operations
{
    [DisplayName("Change YouTrack Issue State")]
    [ScriptAlias("Change-Issue-State")]
    [ScriptNamespace("YouTrack")]
    public sealed class ChangeYouTrackIssueStateOperation : YouTrackOperationBase
    {
        [DisplayName("Credentials")]
        [ScriptAlias("Credentials")]
        public override string CredentialName { get; set; }

        [Required]
        [DisplayName("Issue ID")]
        [ScriptAlias("IssueId")]
        public string IssueId { get; set; }

        [Required]
        [DisplayName("State")]
        [ScriptAlias("State")]
        [SuggestibleValue(typeof(YouTrackStateSuggestionProvider))]
        public string State { get; set; }

        public override async Task ExecuteAsync(IOperationExecutionContext context)
        {
            if (context.Simulation)
            {
                this.LogDebug("Simulating; not changing issue state.");
                return;
            }

            using (var client = this.CreateClient())
            {
                // sanity check to make sure we're not running an arbitrary command
                var states = await client.ListStatesAsync(context.CancellationToken).ConfigureAwait(false);
                if (!states.Contains(this.State, StringComparer.OrdinalIgnoreCase))
                {
                    this.LogError($"No such issue state: {this.State}");
                }

                await client.RunCommandAsync(this.IssueId, $"State {this.State}", null, context.CancellationToken).ConfigureAwait(false);
            }
        }

        protected override ExtendedRichDescription GetDescription(IOperationConfiguration config)
        {
            return new ExtendedRichDescription(new RichDescription(
                "Change YouTrack issue ", new Hilite(config[nameof(IssueId)]),
                " state to ", new Hilite(config[nameof(State)])
            ));
        }
    }
}
