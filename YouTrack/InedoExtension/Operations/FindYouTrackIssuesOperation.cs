using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Inedo.Diagnostics;
using Inedo.Documentation;
using Inedo.ExecutionEngine;
using Inedo.Extensibility;
using Inedo.Extensibility.Operations;
using Inedo.Extensions.YouTrack.SuggestionProviders;
using Inedo.Web;

namespace Inedo.Extensions.YouTrack.Operations
{
    [DisplayName("Find YouTrack Issues")]
    [Description("Queries YouTrack for issues.")]
    [ScriptAlias("Find-Issues")]
    [ScriptNamespace("YouTrack")]
    [Tag("youtrack")]
    public sealed class FindYouTrackIssuesOperation : YouTrackOperationBase
    {
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
        [Description("The output variable should be a list variable. For example: @YouTrackIssueIDs")]
        public RuntimeValue Output { get; set; }

        [Category("Advanced")]
        [DisplayName("Include details in output")]
        [ScriptAlias("IncludeDetails")]
        [Description("When true, the output variable is a list of maps with id, title, and description properties. When false, the output variable is a list of id strings.")]
        public bool IncludeDetails { get; set; }

        public override async Task ExecuteAsync(IOperationExecutionContext context)
        {
            this.LogInformation($"Finding issues in project {this.Project}...");
            if (!string.IsNullOrEmpty(this.Filter))
                this.LogDebug($"Using filter: {this.Filter}");

            using var client = this.CreateClient(context);
            var issues = (await client.IssuesByProjectAsync(this.Project, this.Filter, context.CancellationToken)).ToList();

            if (this.IncludeDetails)
            {
                this.Output = new RuntimeValue(
                    issues.Select(
                        i => new RuntimeValue(
                            new Dictionary<string, RuntimeValue>
                            {
                                ["id"] = i.Id,
                                ["title"] = i.Title ?? string.Empty,
                                ["description"] = i.Description ?? string.Empty
                            }
                        )
                    ).ToList()
                );
            }
            else
            {
                this.Output = new RuntimeValue(issues.Select(i => new RuntimeValue(i.Id)).ToList());
            }

            this.LogInformation($"Found {issues.Count} issues.");
        }

        protected override ExtendedRichDescription GetDescription(IOperationConfiguration config)
        {
            var extended = new RichDescription();

            if (!string.IsNullOrEmpty(config[nameof(Filter)]))
                extended.AppendContent("filtering by ", new Hilite(config[nameof(Filter)]));

            return new ExtendedRichDescription(
                new RichDescription(
                    "Find YouTrack issues in ", new Hilite(config[nameof(Project)]),
                    " to ", new Hilite(config[nameof(Output)].ToString())
                ), extended
            );
        }
    }
}
