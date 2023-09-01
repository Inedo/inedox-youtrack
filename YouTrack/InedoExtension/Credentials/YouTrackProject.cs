using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Inedo.Diagnostics;
using Inedo.Documentation;
using Inedo.ExecutionEngine.Variables;
using Inedo.Extensibility.Credentials;
using Inedo.Extensibility.IssueTrackers;
using Inedo.Serialization;

#nullable enable

namespace Inedo.Extensions.YouTrack.Credentials;

[DisplayName("YouTrack Project")]
[Description("Work with issues on a YouTrack project")]
public class YouTrackProject : IssueTrackerProject<YouTrackServiceCredentials>, IVersionFieldProvider
{
    [Persistent]
    [Category("Advanced Mapping")]
    [DisplayName("Issue mapping query")]
    [PlaceholderText("Fix version: «release-mapping-expression»")]
    public string? CustomMappingQuery { get; set; }
    [Persistent]
    [Category("Advanced Mapping")]
    [DisplayName("Issue status field name")]
    [PlaceholderText("State")]
    public string? IssueStatusFieldName { get; set; }
    [Persistent]
    [Category("Advanced Mapping")]
    [DisplayName("Issue type field name")]
    [PlaceholderText("Type")]
    public string? IssueTypeFieldName { get; set; }
    [Persistent]
    [Category("Advanced Mapping")]
    [DisplayName("Version field name")]
    [PlaceholderText("Fix version")]
    public string? VersionFieldName { get; set; }

    public override async Task<IssuesQueryFilter> CreateQueryFilterAsync(IVariableEvaluationContext context)
    {
        if (!string.IsNullOrEmpty(this.CustomMappingQuery))
        {
            try
            {
                var query = (await ProcessedString.Parse(this.CustomMappingQuery).EvaluateValueAsync(context).ConfigureAwait(false)).AsString();
                if (string.IsNullOrEmpty(query))
                    throw new InvalidOperationException("resulting query is an empty string");
                return new YouTrackIssuesQueryFilter(query);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Could not parse the Issue mapping query \"{this.CustomMappingQuery}\": {ex.Message}");
            }
        }

        try
        {
            var expression = (await ProcessedString.Parse(AH.CoalesceString(this.SimpleVersionMappingExpression, "$ReleaseNumber")).EvaluateValueAsync(context).ConfigureAwait(false)).AsString();
            if (string.IsNullOrEmpty(expression))
                throw new InvalidOperationException("resulting expression is an empty string");
            return new YouTrackIssuesQueryFilter($"Fix version: {expression}");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Could not parse the simple mapping expression \"{this.SimpleVersionMappingExpression}\": {ex.Message}");
        }
    }


    private YouTrackClient CreateClient(ICredentialResolutionContext context)
    {
        var creds = this.GetCredentials(context) as YouTrackServiceCredentials
            ?? throw new InvalidOperationException("Credentials are required to query YouTrack API.");
        if (string.IsNullOrEmpty(creds.ServiceUrl))
            throw new InvalidOperationException($"ServiceUrl must be set on \"{this.CredentialName}\" is required to query YouTrack API.");
        var token = AH.Unprotect(creds.PermanentToken)
            ?? throw new InvalidOperationException($"ServiceUrl must be set on \"{this.CredentialName}\" is required to query YouTrack API.");

        return new YouTrackClient(token, creds.ServiceUrl, this);
    }

    public override Task EnsureVersionAsync(IssueTrackerVersion version, ICredentialResolutionContext context, CancellationToken cancellationToken = default)
        => this.CreateClient(context).EnsureVersionAsync(this.VersionFieldName ?? "Fix version", this.ProjectName!, version.Version, version.IsClosed, version.IsClosed, cancellationToken);

    public async override IAsyncEnumerable<IssueTrackerIssue> EnumerateIssuesAsync(IIssuesEnumerationContext context, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var query = (YouTrackIssuesQueryFilter)context.Filter;
        await foreach (var issue in this.CreateClient(context).EnumerateIssuesAsync(this.ProjectName, query.Search, this.IssueStatusFieldName ?? "State", this.IssueTypeFieldName ?? "Type", cancellationToken))
            yield return new IssueTrackerIssue(issue.ReadableId, issue.Status, issue.Type, issue.Summary, issue.Description, issue.Reporter, issue.Created, issue.Resolved.HasValue, issue.Url);
    }
    public IAsyncEnumerable<string> EnumerateVersionFieldsAsync(ICredentialResolutionContext context, CancellationToken cancellationToken = default)
        => this.CreateClient(context).EnumerateVersionFieldsAsync(this.ProjectName!, cancellationToken);

    public override RichDescription GetDescription() => new(this.ProjectName);

    public override IAsyncEnumerable<IssueTrackerVersion> EnumerateVersionsAsync(ICredentialResolutionContext context, CancellationToken cancellationToken = default)
    {
        if (this.ProjectName == null)
            throw new YouTrackException("ProjectName is not set.");

        return this.CreateClient(context).EnumerateVersionsAsync(this.VersionFieldName ?? "Fix version", this.ProjectName, cancellationToken);
    }
    public override async Task<IReadOnlyList<IssueTrackerVersion>> GetRecentVersionsAsync(ICredentialResolutionContext context, int count = 5, CancellationToken cancellationToken = default)
    {
        this.LogDebug("YouTrack API does not support sorting of versions, so enumerating all versions to find the latest...");
        var allVersions = await this.EnumerateVersionsAsync(context, cancellationToken).ToListAsync(cancellationToken).ConfigureAwait(false);
        return allVersions.TakeLast(count).Reverse().ToList();
    }

    public async override Task TransitionIssuesAsync(string? fromStatus, string toStatus, string? comment, IIssuesEnumerationContext context, CancellationToken cancellationToken = default)
    {
        var client = this.CreateClient(context);
        var search = ((YouTrackIssuesQueryFilter)context.Filter).Search;
        if (!string.IsNullOrEmpty(fromStatus))
            search = $"({search}) OR {this.IssueStatusFieldName} {fromStatus}";

        var issues = await client.EnumerateIssuesAsync(this.ProjectName, search, this.IssueStatusFieldName ?? "State", this.IssueTypeFieldName ?? "Type", cancellationToken)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        await client.RunCommandAsync($"{this.IssueStatusFieldName} {toStatus}", issues.Select(i => i.ReadableId), comment, cancellationToken);
    }
    private sealed class YouTrackIssuesQueryFilter : IssuesQueryFilter
    {
        public YouTrackIssuesQueryFilter(string search) => this.Search = search;

        public string Search { get; }
    }
}


