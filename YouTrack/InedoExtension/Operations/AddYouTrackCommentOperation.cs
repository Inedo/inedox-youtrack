using Inedo.Diagnostics;
using Inedo.Documentation;
using Inedo.Extensibility;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Inedo.Extensibility.Operations;
using Inedo.Web;

namespace Inedo.Extensions.YouTrack.Operations;

[ScriptAlias("Add-Comment")]
[ScriptNamespace("YouTrack")]
[Description("Adds a comment to a YouTrack issue.")]
[Example(@"# Add a comment to issues in the current release of the current project.
YouTrack::Add-Comment
(
    Query: Project: {$ApplicationName} Fix version: $ReleaseNumber,
    Comment: Comment added by BuildMaster in build $BuildNumber of release $ReleaseNumber of $ApplicationName.
);")]
public sealed class AddYouTrackCommentOperation : YouTrackOperationBase
{
    [ScriptAlias("Query")]
    [Description("The YouTrack issue query. For example, to get open issues for the release currently in context:<br/><br/><code>Project: {$ApplicationName} Fix version: $ReleaseNumber State: -Completed</code> <br/><br/>" +
        "For more information on filters, see: <a target=\"_blank\" href=\"https://www.jetbrains.com/help/youtrack/standalone/Search-and-Command-Attributes.html\">https://www.jetbrains.com/help/youtrack/standalone/Search-and-Command-Attributes.html</a>")]
    public string Query { get; set; }
    [Category("Advanced")]
    [ScriptAlias("Issues")]
    [ScriptAlias("IssueId", Obsolete = true)]
    [DisplayName("Issue IDs")]
    [Description("Apply to the issue IDs directly. When this value is specified, \"Query\" is ignored.")]
    public IEnumerable<string> IssueIds { get; set; }

    [Required]
    [ScriptAlias("Comment")]
    [FieldEditMode(FieldEditMode.Multiline)]
    public string Comment { get; set; }

    private protected override async Task ExecuteAsync(IOperationExecutionContext context, YouTrackClient client)
    {
        if (this.IssueIds == null && string.IsNullOrWhiteSpace(this.Query))
        {
            this.LogError("Missing required argument \"Query\" or \"Issues\".");
            return;
        }

        this.LogInformation($"Adding comment \"{this.Comment}\" to issues...");

        if (context.Simulation)
        {
            this.LogDebug("Simulating; not creating a comment.");
            return;
        }

        var ids = this.IssueIds?.ToList();
        if (ids == null)
        {
            this.LogDebug("Querying YouTrack: " + this.Query);
            ids = (await client.GetIssuesAsync(customQuery: this.Query, cancellationToken: context.CancellationToken))
                .Select(i => i.ReadableId)
                .ToList();
        }

        this.LogDebug("Adding comment to: " + string.Join(", ", ids));
        await client.RunCommandAsync("comment", ids, this.Comment, context.CancellationToken);
        this.LogInformation("Comment added.");
    }

    protected override ExtendedRichDescription GetDescription(IOperationConfiguration config)
    {
        var ids = config[nameof(IssueIds)].AsEnumerable()?.ToList();

        RichDescription desc2;

        if (ids != null && ids.Count > 0)
        {
            desc2 = new RichDescription(
                "to issues ",
                new ListHilite(ids)
            );
        }
        else
        {
            desc2 = new RichDescription(
                "to issues matching query ",
                new Hilite(config[nameof(this.Query)])
            );
        }

        return new ExtendedRichDescription(
            new RichDescription(
                "Add YouTrack comment ",
                new Hilite(config[nameof(Comment)])
            ),
            desc2
        );
    }
}
