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
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace Inedo.Extensions.YouTrack.Operations
{
    [DisplayName("Find YouTrack Issues")]
    [Description("Queries YouTrack issue IDs.")]
    [ScriptAlias("Find-Issues")]
    [ScriptNamespace("YouTrack")]
    [Tag("youtrack")]
    public sealed class FindYouTrackIssuesOperation : YouTrackOperationBase
    {
        [DisplayName("Credentials")]
        [ScriptAlias("Credentials")]
        public override string CredentialName { get; set; }

        [Required]
        [DisplayName("Project name")]
        [ScriptAlias("Project")]
        [SuggestibleValue(typeof(YouTrackProjectSuggestionProvider))]
        public string Project { get; set; }

        [DisplayName("Search filter")]
        [ScriptAlias("Filter")]
        public string Filter { get; set; }

        [Output]
        [Required]
        [DisplayName("Output variable")]
        [ScriptAlias("Output")]
        [PlaceholderText("@IssueIDs")]
        public IEnumerable<string> Output { get; set; }

        public override async Task ExecuteAsync(IOperationExecutionContext context)
        {
            this.LogDebug($"Finding issues in project {this.Project}...");
            if (!string.IsNullOrEmpty(this.Filter))
            {
                this.LogDebug($"Using filter: {this.Filter}");
            }

            using (var client = this.CreateClient())
            {
                var issues = await client.IssuesByProjectAsync(this.Project, this.Filter, context.CancellationToken).ConfigureAwait(false);

                var ids = issues.Select(i => i.Id).ToList();
                this.Output = ids;

                this.LogDebug($"Found {ids.Count} issues.");
            }
        }

        protected override ExtendedRichDescription GetDescription(IOperationConfiguration config)
        {
            var extended = new RichDescription();

            if (!string.IsNullOrEmpty(config[nameof(Filter)]))
            {
                extended.AppendContent("filtering by ", new Hilite(config[nameof(Filter)]));
            }

            return new ExtendedRichDescription(
                new RichDescription(
                    "Find YouTrack issues in ", new Hilite(config[nameof(Project)]),
                    " to ", new Hilite(config[nameof(Output)].ToString())
                ), extended
            );
        }
    }
}
