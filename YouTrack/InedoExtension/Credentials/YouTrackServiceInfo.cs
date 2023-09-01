using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Inedo.Extensibility.IssueTrackers;
using Inedo.Web;

#nullable enable

namespace Inedo.Extensions.YouTrack.Credentials;

[DisplayName("YouTrack")]
[Description("Provides integration for YouTrack issue tracking projects.")]
public sealed class YouTrackServiceInfo : IssueTrackerService<YouTrackProject, YouTrackServiceCredentials>
{
    public override string ServiceName => "YouTrack";
    public override string PasswordDisplayName => "Permanent token";
    public override string ApiUrlDisplayName => "YouTrack server URL";
    public override string ApiUrlPlaceholderText => "https://example.myjetbrains.com/youtrack/";
    public override string DefaultVersionFieldName => "Fix version";
    public override string VersionClosedDescription => "Version is \"Archived\" and \"Released\"";

    protected override async IAsyncEnumerable<string> GetProjectNamesAsync(YouTrackServiceCredentials credentials, string? serviceNamespace = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(credentials.ServiceUrl))
            throw new InvalidOperationException($"ServiceUrl must be set to query YouTrack API.");
        var token = AH.Unprotect(credentials.PermanentToken)
            ?? throw new InvalidOperationException($"Token must be set to query YouTrack API.");

        var client = new YouTrackClient(token, credentials.ServiceUrl);
        await foreach (var p in client.GetProjectsAsync(cancellationToken).ConfigureAwait(false))
            yield return p.Name;
    }

    protected override async Task<ValidationResults> ValidateProjectAsync(YouTrackServiceCredentials credentials, string projectName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(credentials.ServiceUrl))
            throw new InvalidOperationException($"ServiceUrl must be set to query YouTrack API.");
        var token = AH.Unprotect(credentials.PermanentToken)
            ?? throw new InvalidOperationException($"Token must be set to query YouTrack API.");

        var client = new YouTrackClient(token, credentials.ServiceUrl);
        try
        {
            if (await client.EnumerateVersionFieldsAsync(projectName, cancellationToken).AnyAsync(cancellationToken).ConfigureAwait(false))
                return true;

            return new(false, "The project must have at least one version-type field, such as \"Fix version\".");
        }
        catch (Exception ex)
        {
            return new ValidationResults(false, "Error:" + ex.Message);
        }
    }
}
