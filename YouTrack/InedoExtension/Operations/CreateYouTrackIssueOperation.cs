using System.ComponentModel;
using System.Threading.Tasks;
using Inedo.Diagnostics;
using Inedo.Documentation;
using Inedo.Extensibility;
using Inedo.Extensibility.Operations;
using Inedo.Extensions.YouTrack.SuggestionProviders;
using Inedo.Web;

namespace Inedo.Extensions.YouTrack.Operations
{
    [Tag("youtrack")]
    [ScriptAlias("Create-Issue")]
    [ScriptNamespace("YouTrack")]
    [DisplayName("Create YouTrack Issue")]
    [Description("Creates a new issue in YouTrack.")]
    [Example(@"# Creates an example issue in the $ApplicationName YouTrack project and stores its issue ID in the $issueId variable.
YouTrack::Create-Issue
(
    Project: $ApplicationName,
    Summary: Example issue,
    Description: >>This issue was created by BuildMaster.
It is just an example.>>,
    IssueId => $issueId
);

# Write the issue ID to the execution log.
Log-Information Created issue $issueId in YouTrack.;")]
    public sealed class CreateYouTrackIssueOperation : YouTrackOperationBase
    {
        [Required]
        [ScriptAlias("Project")]
        [SuggestableValue(typeof(YouTrackProjectSuggestionProvider))]
        public string Project { get; set; }

        [Required]
        [ScriptAlias("Summary")]
        public string Summary { get; set; }

        [ScriptAlias("Description")]
        [FieldEditMode(FieldEditMode.Multiline)]
        public string Description { get; set; }

        [Output]
        [DisplayName("Issue ID")]
        [ScriptAlias("IssueId")]
        [PlaceholderText("ex: $issueId")]
        public string IssueId { get; set; }

        private protected override async Task ExecuteAsync(IOperationExecutionContext context, YouTrackClient client)
        {
            this.LogInformation($"Creating issue in project {this.Project} with summary: {this.Summary}...");
            this.LogDebug("Description: " + this.Description);

            if (context.Simulation)
            {
                this.LogDebug("Simulating; not creating an issue.");
                this.IssueId = "ABC-123";
                return;
            }

            this.IssueId = await client.CreateIssueAsync(this.Project, this.Summary, this.Description, context.CancellationToken);
        }

        protected override ExtendedRichDescription GetDescription(IOperationConfiguration config)
        {
            return new ExtendedRichDescription(new RichDescription(
                "Create YouTrack issue in ", new Hilite(config[nameof(Project)]),
                ": ", new Hilite(config[nameof(Summary)])
            ), new RichDescription(config[nameof(Description)]));
        }
    }
}
