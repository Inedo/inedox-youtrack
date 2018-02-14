using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Inedo.Diagnostics;
using Inedo.Documentation;
using Inedo.Extensibility;
using Inedo.Extensibility.Operations;
using Inedo.Extensions.YouTrack.SuggestionProviders;
using Inedo.Web;

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
        [DisplayName("Project ID")]
        [ScriptAlias("Project")]
        [SuggestableValue(typeof(YouTrackProjectSuggestionProvider))]
        public string Project { get; set; }

        [DisplayName("Search filter")]
        [ScriptAlias("Filter")]
        [Description("The YouTrack issue filter. for example, to get open issues for the release currently in context:<br/><br/><code>Fix version: $ReleaseNumber State: -Completed</code> <br/><br/>" +
            "For more information on filters, see: <a target=\"_blank\" href=\"https://www.jetbrains.com/help/youtrack/standalone/Issue-Filters.html\">https://www.jetbrains.com/help/youtrack/standalone/Issue-Filters.html</a>")]
        public string Filter { get; set; }

        [Output]
        [DisplayName("Output variable")]
        [ScriptAlias("Output")]
        [Description("The output variable should be a list variable, for example: @YouTrackIssueIDs")]
        public IEnumerable<string> Output { get; set; }

        public override async Task ExecuteAsync(IOperationExecutionContext context)
        {
            this.LogInformation($"Finding issues in project {this.Project}...");
            if (!string.IsNullOrEmpty(this.Filter))
            {
                this.LogDebug($"Using filter: {this.Filter}");
            }

            using (var client = this.CreateClient())
            {
                var issues = await client.IssuesByProjectAsync(this.Project, this.Filter, context.CancellationToken).ConfigureAwait(false);

                var ids = issues.Select(i => i.Id).ToList();
                this.Output = ids;

                this.LogInformation($"Found {ids.Count} issues.");
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
