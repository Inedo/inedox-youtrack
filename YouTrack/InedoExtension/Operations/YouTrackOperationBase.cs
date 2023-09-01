#nullable enable

using System.ComponentModel;
using System.Security;
using Inedo.Diagnostics;
using Inedo.Documentation;
using System.Threading.Tasks;
using Inedo.ExecutionEngine.Executer;
using Inedo.Extensibility;
using Inedo.Extensibility.Credentials;
using Inedo.Extensibility.Operations;
using Inedo.Extensibility.SecureResources;
using Inedo.Extensions.YouTrack.Credentials;
using Inedo.Web;

namespace Inedo.Extensions.YouTrack.Operations;

public abstract class YouTrackOperationBase : ExecuteOperation
{
    protected YouTrackOperationBase()
    {
    }

    [DisplayName("From project")]
    [ScriptAlias("From")]
    [ScriptAlias("Credentials")]
    [SuggestableValue(typeof(SecureResourceSuggestionProvider<YouTrackProject>))]
    public string? ResourceName { get; set; }

    [Category("Connection")]
    [DisplayName("YouTrack server URL")]
    [ScriptAlias("Server")]
    [PlaceholderText("Use server URL from resource")]
    public string? ServerUrl { get; set; }

    [Undisclosed]
    [ScriptAlias("CredentialName")]
    public string? CredentialName { get; set; }

    [Category("Connection")]
    [DisplayName("Permanent token")]
    [ScriptAlias("PermanentToken")]
    [PlaceholderText("Use permanent token from credential")]
    public SecureString? PermanentToken { get; set; }

    public sealed override async Task ExecuteAsync(IOperationExecutionContext context)
    {
        try
        {
            await this.ExecuteAsync(context, this.GetClient(context));
        }
        catch (YouTrackException ex)
        {
            this.LogError(ex.Message);
        }
    }

    private protected abstract Task ExecuteAsync(IOperationExecutionContext context, YouTrackClient client);

    private YouTrackClient GetClient(IOperationExecutionContext context)
    {
        YouTrackServiceCredentials credentials; YouTrackProject project;
        if (string.IsNullOrEmpty(this.ResourceName))
        {
            if (string.IsNullOrEmpty(this.ServerUrl))
                throw new ExecutionFailureException($"Server Url must be specified if a Resource is not specified.");
            if (string.IsNullOrEmpty(this.CredentialName) && this.PermanentToken == null)
                throw new ExecutionFailureException($"PermanentToken must be specified when specifying a server url..");

            project = new();
            credentials = new() { ServiceUrl = this.ServerUrl };
        }
        else
        {
            var maybeProject = SecureResource.TryCreate(SecureResourceType.IssueTrackerProject, this.ResourceName, context);
            if (maybeProject is YouTrackProject)
            {
                project = (YouTrackProject)maybeProject;
                credentials = (YouTrackServiceCredentials?)project.GetCredentials(context) ?? new();
            }
            else
            {
                maybeProject = SecureResource.TryCreate(SecureResourceType.General, this.ResourceName, context);
                if (maybeProject is not YouTrackSecureResource legacyProject)
                    throw new ExecutionFailureException($"Resource \"{this.ResourceName}\" does not reference a YouTrack project resource.");

                this.LogWarning(
                    $"The specified resource ({this.ResourceName}) is a legacy {nameof(YouTrackSecureResource)} and will be removed in a future version. " +
                    $"You should delete, then recreate \"{this.ResourceName}\" as a supported {nameof(YouTrackProject)} resource.");
                var legacyCreds = legacyProject.GetCredentials(context) as YouTrackTokenCredentials;

                project = new();
                credentials = new YouTrackServiceCredentials { ServiceUrl = legacyProject.ServerUrl, PermanentToken = legacyCreds?.PermanentToken };
            }
        }

        if (!string.IsNullOrEmpty(this.CredentialName))
        {
            this.LogWarning("The Credential property has been deprecated. Specify a Token as a property instead.");
            if (SecureCredentials.Create(this.CredentialName, context) is not YouTrackTokenCredentials legacyCreds)
                throw new ExecutionFailureException($"Credential \"{this.CredentialName}\" is not a {nameof(YouTrackTokenCredentials)}.");
            credentials.PermanentToken = legacyCreds.PermanentToken;
        }
        if (this.PermanentToken != null)
            credentials.PermanentToken = this.PermanentToken;

        if (credentials.PermanentToken == null)
            throw new ExecutionFailureException($"A PermamentToken was not specified on the operation or credential.");
        if (string.IsNullOrEmpty(credentials.ServiceUrl))
            throw new ExecutionFailureException($"A ServerUrl was not specified on the operation or credential.");


        return new YouTrackClient(AH.Unprotect(credentials.PermanentToken), credentials.ServiceUrl, this, project.ProjectName);
    }
}
