using Inedo.Diagnostics;
using Inedo.Documentation;
using Inedo.Extensibility;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Inedo.Extensibility.Operations;

namespace Inedo.Extensions.YouTrack.Operations;

[ScriptAlias("Run-Command")]
[Description("Runs a YouTrack command against one or more issues.")]
[Example(@"# Applies the 'my-tag' tag to issues matching the specified query.
YouTrack::Run-Command
(
    Query: Project: {$ApplicationName} Fix version: $ReleaseNumber,
    Command: tag my-tag
);")]
public sealed class RunYouTrackCommandOperation : YouTrackOperationBase
{
    [ScriptAlias("Query")]
    [Description("The YouTrack issue query. For example, to get open issues for the release currently in context:<br/><br/><code>Project: {$ApplicationName} Fix version: $ReleaseNumber State: -Completed</code> <br/><br/>" +
        "For more information on filters, see: <a target=\"_blank\" href=\"https://www.jetbrains.com/help/youtrack/standalone/Search-and-Command-Attributes.html\">https://www.jetbrains.com/help/youtrack/standalone/Search-and-Command-Attributes.html</a>")]
    public string Query { get; set; }
    [Category("Advanced")]
    [ScriptAlias("Issues")]
    [DisplayName("Issue IDs")]
    [Description("Apply to the issue IDs directly. When this value is specified, \"Query\" is ignored.")]
    public IEnumerable<string> IssueIds { get; set; }
    [Required]
    [ScriptAlias("Command")]
    public string Command { get; set; }
    [ScriptAlias("Comment")]
    [PlaceholderText("none")]
    public string Comment { get; set; }

    private protected override async Task ExecuteAsync(IOperationExecutionContext context, YouTrackClient client)
    {
        if (string.IsNullOrWhiteSpace(this.Command))
        {
            this.LogError("Missing required value: Command");
            return;
        }

        if (context.Simulation)
        {
            this.LogDebug("Simulating; not running command.");
            return;
        }

        if (this.IssueIds == null && string.IsNullOrWhiteSpace(this.Query))
        {
            this.LogError("Missing required argument \"Query\" or \"Issues\".");
            return;
        }

        var ids = this.IssueIds?.ToList();
        if (ids == null)
        {
            ids = (await client.GetIssuesAsync(customQuery: this.Query, cancellationToken: context.CancellationToken))
                .Select(i => i.ReadableId)
                .ToList();
        }

        await client.RunCommandAsync(this.Command, ids, this.Comment, context.CancellationToken);
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
                "Run YouTrack command ",
                new Hilite(config[nameof(Command)])
            ),
            desc2
        );
    }
}
