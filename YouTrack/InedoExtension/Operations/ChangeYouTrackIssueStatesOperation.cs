using Inedo.Diagnostics;
using Inedo.Documentation;
using Inedo.Extensibility;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Inedo.Extensibility.Operations;

namespace Inedo.Extensions.YouTrack.Operations;

[ScriptNamespace("YouTrack")]
[ScriptAlias("Change-IssueStates")]
[ScriptAlias("Change-Issue-State", Obsolete = true)]
[Description("Changes the state of YouTrack issues.")]
[Example(@"# Change the state of all issues in the application's project with 'Fix version' = $ReleaseNumber to Completed.
YouTrack::ChangeIssueStates
(
    Query: Project: {$ApplicationName} Fix version: $ReleaseNumber,
    State: Completed
);")]
public sealed class ChangeYouTrackIssueStatesOperation : YouTrackOperationBase
{
    [ScriptAlias("Query")]
    [Description("The YouTrack issue query. For example, to get open issues for the release currently in context:<br/><br/><code>Project: {$ApplicationName} Fix version: $ReleaseNumber State: -Completed</code> <br/><br/>" +
        "For more information on filters, see: <a target=\"_blank\" href=\"https://www.jetbrains.com/help/youtrack/standalone/Search-and-Command-Attributes.html\">https://www.jetbrains.com/help/youtrack/standalone/Search-and-Command-Attributes.html</a>")]
    public string Query { get; set; }

    [Category("Advanced")]
    [DisplayName("Issue IDs")]
    [ScriptAlias("Issues")]
    [ScriptAlias("IssueId", Obsolete = true)]
    [Description("Apply to the issue IDs directly. When this value is specified, \"Query\" is ignored.")]
    public IEnumerable<string> IssueIds { get; set; }

    [Required]
    [ScriptAlias("State")]
    public string State { get; set; }

    [ScriptAlias("Comment")]
    [PlaceholderText("none")]
    public string Comment { get; set; }

    [Category("Advanced")]
    [ScriptAlias("IssueStatusFieldName")]
    [DefaultValue("$YouTrackStatusFieldName")]
    [DisplayName("Issue status custom field")]
    public string IssueStatusFieldName { get; set; } = "$YouTrackStatusFieldName";

    private protected override async Task ExecuteAsync(IOperationExecutionContext context, YouTrackClient client)
    {
        if (this.IssueIds == null && string.IsNullOrWhiteSpace(this.Query))
        {
            this.LogError("Missing required argument \"Query\" or \"Issues\".");
            return;
        }

        this.LogInformation($"Changinge state on issues to {this.State}...");

        if (context.Simulation)
        {
            this.LogDebug("Simulating; not changing issue states.");
            return;
        }

        var ids = this.IssueIds?.ToList();
        if (ids == null)
        {
            this.LogDebug("Querying YouTrack: " + this.Query);
            ids = (await client.GetIssuesAsync(customQuery: this.Query, statusField: this.IssueStatusFieldName, cancellationToken: context.CancellationToken))
                .Select(i => i.ReadableId)
                .ToList();
        }

        this.LogDebug("Changing state on: " + string.Join(", ", ids));
        await client.RunCommandAsync($"{this.IssueStatusFieldName} {this.State}", ids, this.Comment, context.CancellationToken);
        this.LogInformation("State change applied.");
    }

    protected override ExtendedRichDescription GetDescription(IOperationConfiguration config)
    {
        var ids = config[nameof(IssueIds)].AsEnumerable()?.ToList();

        RichDescription desc2;

        if (ids != null && ids.Count > 0)
        {
            desc2 = new RichDescription(
                "for issues ",
                new ListHilite(ids)
            );
        }
        else
        {
            desc2 = new RichDescription(
                "for issues matching query ",
                new Hilite(config[nameof(this.Query)])
            );
        }

        return new ExtendedRichDescription(
            new RichDescription(
                "Change YouTrack issues' states to ",
                new Hilite(config[nameof(State)])
            ),
            desc2
        );
    }
}
