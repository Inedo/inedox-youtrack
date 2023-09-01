using Inedo.Diagnostics;
using Inedo.Documentation;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Inedo.ExecutionEngine;
using Inedo.Extensibility;
using Inedo.Extensibility.Operations;
using System.Collections.Generic;

namespace Inedo.Extensions.YouTrack.Operations;

[Description("Queries YouTrack for issues and stores information about them in an OtterScript variable.")]
[ScriptAlias("Query-Issues")]
[ScriptAlias("Find-Issues", Obsolete = true)]
[ScriptNamespace("YouTrack")]
[Example(@"# Queries YouTrack for open issues with 'Fix version' = $ReleaseNumber for the $ApplicationName project and stores the issue IDs in the @issueIds variable.
YouTrack::Query-Issues
(
    Query: Project: {$ApplicationName} Fix version: $ReleaseNumber State: Open,
    Output => @issueIds
);")]
public sealed class QueryYouTrackIssuesOperation : YouTrackOperationBase
{
    [Category("Advanced")]
    [ScriptAlias("Project")]
    public string Project { get; set; }

    [DisplayName("Query")]
    [ScriptAlias("Query")]
    [ScriptAlias("Filter", Obsolete = true)]
    [Description("The YouTrack issue query. For example, to get open issues for the release currently in context:<br/><br/><code>Project: {$ApplicationName} Fix version: $ReleaseNumber State: -Completed</code> <br/><br/>" +
        "For more information on filters, see: <a target=\"_blank\" href=\"https://www.jetbrains.com/help/youtrack/standalone/Search-and-Command-Attributes.html\">https://www.jetbrains.com/help/youtrack/standalone/Search-and-Command-Attributes.html</a>")]
    public string Query { get; set; }

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

    [Category("Advanced")]
    [ScriptAlias("IssueStatusFieldName")]
    [DefaultValue("$YouTrackStatusFieldName")]
    [DisplayName("Issue status custom field")]
    public string IssueStatusFieldName { get; set; } = "$YouTrackStatusFieldName";
    [Category("Advanced")]
    [ScriptAlias("IssueTypeFieldName")]
    [DefaultValue("$YouTrackTypeFieldName")]
    [DisplayName("Issue type custom field")]
    public string IssueTypeFieldName { get; set; } = "$YouTrackTypeFieldName";

    private protected override async Task ExecuteAsync(IOperationExecutionContext context, YouTrackClient client)
    {
        var issues = await client.GetIssuesAsync(this.Project, this.Query, this.IssueStatusFieldName, this.IssueTypeFieldName, context.CancellationToken);
        if (this.IncludeDetails)
        {
            this.Output = new RuntimeValue(
                issues.Select(
                    i => new RuntimeValue(
                        new Dictionary<string, RuntimeValue>
                        {
                            ["id"] = i.ReadableId,
                            ["title"] = i.Summary ?? string.Empty,
                            ["description"] = i.Description ?? string.Empty
                        }
                    )
                ).ToList()
            );
        }
        else
        {
            this.Output = new RuntimeValue(issues.Select(i => new RuntimeValue(i.ReadableId)).ToList());
        }

        this.LogInformation($"Found {issues.Count} issues.");
    }

    protected override ExtendedRichDescription GetDescription(IOperationConfiguration config)
    {
        var extended = new RichDescription();

        if (!string.IsNullOrEmpty(config[nameof(Query)]))
            extended.AppendContent("filtering by ", new Hilite(config[nameof(Query)]));

        return new ExtendedRichDescription(
            new RichDescription(
                "Find YouTrack issues in ", new Hilite(config[nameof(Project)]),
                " to ", new Hilite(config[nameof(Output)].ToString())
            ), extended
        );
    }
}
