#nullable enable

using System.Collections.Generic;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Inedo.Diagnostics;
using System.Threading.Tasks;
using System.Threading;
using Inedo.Extensibility.IssueTrackers;


namespace Inedo.Extensions.YouTrack;

internal sealed class YouTrackClient : ILogSink
{
    private readonly ILogSink? log;

    public YouTrackClient(string token, string apiUrl, ILogSink? log = null, string? projectName = null)
    {
        this.log = log;
        this.Token = token;
        this.ApiUrl = MakeUrlCanonical(apiUrl);
        this.DefaultProjectName = projectName;
    }

    public string Token { get; }
    public string ApiUrl { get; }
    public string? DefaultProjectName { get; }

    public IAsyncEnumerable<YouTrackProjectInfo> GetProjectsAsync(CancellationToken cancellationToken = default)
    {
        return this.GetPaginatedListAsync("admin/projects?fields=id,name,shortName", getProjects, cancellationToken);

        IEnumerable<YouTrackProjectInfo> getProjects(JsonDocument doc)
        {
            foreach (var obj in doc.RootElement.EnumerateArray())
            {
                if (!this.TryGetString(obj, "id", out var id)
                    ||
                    !this.TryGetString(obj, "name", out var name)
                    ||
                    !this.TryGetString(obj, "shortName", out var shortName))
                    continue;

                yield return new YouTrackProjectInfo(id, name, shortName);
            }
        }
    }
    public async Task<string> CreateIssueAsync(string projectName, string summary, string description, CancellationToken cancellationToken = default)
    {
        var project = await this.GetProjectAsync(projectName, cancellationToken).ConfigureAwait(false);

        using var request = this.CreateRequest(HttpMethod.Post, "issues?fields=idReadable");
        using var buffer = new MemoryStream();
        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartObject();

            writer.WritePropertyName("project");
            writer.WriteStartObject();
            writer.WriteString("id", project.Id);
            writer.WriteEndObject();

            writer.WriteString("summary", summary);
            writer.WriteString("description", description);

            writer.WriteEndObject();
        }

        buffer.Position = 0;
        request.Content = new StreamContent(buffer);
        request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

        using var response = await GetResponseAsync(request, cancellationToken).ConfigureAwait(false);
        using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var doc = await JsonDocument.ParseAsync(responseStream, cancellationToken: cancellationToken).ConfigureAwait(false);

        var id = doc.RootElement.GetProperty("idReadable").GetString()!;
        this.LogInformation($"Created issue {id}.");
        return id;
    }
    public async IAsyncEnumerable<YouTrackIssue> EnumerateIssuesAsync(string? projectName = null, string? customQuery = null, string statusField = "State", string typeField = "Type", [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var query = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(projectName))
        {
            query.Append("project: {");
            query.Append(projectName);
            query.Append('}');
        }

        if (!string.IsNullOrWhiteSpace(customQuery))
        {
            if (query.Length > 0)
                query.Append(' ');

            query.Append(customQuery);
        }

        var tokens = this.GetPaginatedListAsync(
            "issues?fields=id,idReadable,summary,description,reporter(fullName),created,resolved,customFields(name,value(name))&query=" + Uri.EscapeDataString(query.ToString()),
            getObjects,
            cancellationToken
        );

        var baseUrl = this.ApiUrl.TrimEnd('/');
        baseUrl = baseUrl[..baseUrl.LastIndexOf('/')] + "/issue/";

        await foreach (var obj in tokens.ConfigureAwait(false))
        {
            var id = (string)obj["idReadable"]!;
            if (!string.IsNullOrWhiteSpace(id))
            {
                if (!this.TryReadCustomFieldValue(obj, statusField, out var status)
                    ||
                    !this.TryReadCustomFieldValue(obj, typeField, out var type))
                    continue;

                yield return new YouTrackIssue(id, obj, baseUrl + id, status, type);
            }
        }

        static IEnumerable<JsonObject> getObjects(JsonDocument doc)
        {
            foreach (var obj in doc.RootElement.EnumerateArray())
                yield return JsonObject.Create(obj)!;
        }
    }
    public async Task<IReadOnlyCollection<YouTrackIssue>> GetIssuesAsync(string? projectName = null, string? customQuery = null, string statusField = "State", string typeField = "Type", CancellationToken cancellationToken = default)
    {
        var issues = new List<YouTrackIssue>();
        await foreach (var issue in EnumerateIssuesAsync(projectName, customQuery, statusField, typeField, cancellationToken).ConfigureAwait(false))
            issues.Add(issue);

        return issues;
    }
    public async Task RunCommandAsync(string command, IEnumerable<string> issueIds, string? comment = null, CancellationToken cancellationToken = default)
    {
        var idList = issueIds as IReadOnlyCollection<string> ?? issueIds.ToList();
        if (idList.Count == 0)
            return;

        using var request = this.CreateRequest(HttpMethod.Post, "commands");

        using var buffer = new MemoryStream();
        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartObject();

            writer.WriteString("query", command);

            if (!string.IsNullOrWhiteSpace(comment))
                writer.WriteString("comment", comment);

            writer.WritePropertyName("issues");
            writer.WriteStartArray();
            foreach (var i in idList)
            {
                writer.WriteStartObject();
                writer.WriteString("idReadable", i);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();

            writer.WriteEndObject();
        }

        buffer.Position = 0;
        request.Content = new StreamContent(buffer);
        request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

        using var response = await GetResponseAsync(request, cancellationToken).ConfigureAwait(false);
    }
    public async IAsyncEnumerable<string> EnumerateVersionFieldsAsync(string projectName, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var project = await this.GetProjectAsync(projectName, cancellationToken).ConfigureAwait(false);
        await foreach (var field in this.EnumerateVersionFieldsAsync(project, cancellationToken).ConfigureAwait(false))
            yield return field.Name;
    }
    public async IAsyncEnumerable<IssueTrackerVersion> EnumerateVersionsAsync(string versionField, string projectName, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var project = await this.GetProjectAsync(projectName, cancellationToken).ConfigureAwait(false);
        var customFieldId = await this.GetFieldIdAsync(project, versionField, cancellationToken).ConfigureAwait(false);

        this.LogDebug("Fetching custom field values...");
        var customFieldUrl = $"admin/projects/{project.Id}/customFields/{customFieldId}/bundle/values?fields=id,name,released";

        await foreach (var v in this.GetPaginatedListAsync(customFieldUrl, getIssues, cancellationToken).ConfigureAwait(false))
            yield return v;

        static IEnumerable<IssueTrackerVersion> getIssues(JsonDocument doc)
        {
            foreach (var obj in doc.RootElement.EnumerateArray())
            {
                if (obj.TryGetProperty("name", out var nameElement))
                {
                    bool released = obj.TryGetProperty("released", out var releasedElement) && releasedElement.ValueKind == JsonValueKind.True;
                    yield return new IssueTrackerVersion(nameElement.ToString(), released);
                }
            }
        }
    }
    public async Task EnsureVersionAsync(string versionField, string projectName, string version, bool? released, bool? archived, CancellationToken cancellationToken)
    {
        var project = await this.GetProjectAsync(projectName, cancellationToken).ConfigureAwait(false);
        var customFieldId = await this.GetFieldIdAsync(project, versionField, cancellationToken).ConfigureAwait(false);

        this.LogDebug("Fetching custom field values...");
        var customFieldUrl = $"admin/projects/{project.Id}/customFields/{customFieldId}/bundle/values?fields=id,name,released,archived";

        var data = await FirstOrDefaultAsync(
            this.GetPaginatedListAsync(
                customFieldUrl,
                d => getCustomField(d, version),
                cancellationToken
            )
        ).ConfigureAwait(false);

        bool update = false;

        if (data == null)
        {
            this.LogDebug($"Custom field value {version} does not already exist.");

            data = new JsonObject
            {
                ["name"] = version,
                ["released"] = released ?? false,
                ["archived"] = archived ?? false,
                ["type"] = "$VersionBundleElement"
            };

            update = true;
        }
        else
        {
            this.LogDebug($"Custom field value {version} already exists.");

            if ((bool?)data["released"] != released || (bool?)data["archived"] != archived)
            {
                if (released.HasValue)
                {
                    this.LogDebug($"Setting released to {released} (current value is {data["released"]})...");
                    data["released"] = released.GetValueOrDefault();
                    update = true;
                }

                if (archived.HasValue)
                {
                    this.LogDebug($"Setting archived to {archived} (current value is {data["archived"]})...");
                    data["archived"] = archived.GetValueOrDefault();
                    update = true;
                }
            }
        }

        if (update)
        {
            this.LogDebug($"Creating/updating version {version}...");

            using var request = this.CreateRequest(HttpMethod.Post, $"admin/projects/{project.Id}/customFields/{customFieldId}/bundle/values/{(string?)data["id"]}");
            request.Content = new StringContent(data.ToJsonString(), InedoLib.UTF8Encoding, "application/json");

            using var response = await GetResponseAsync(request, cancellationToken).ConfigureAwait(false);
            this.LogInformation($"Version {version} update complete.");
        }
        else
        {
            this.LogInformation($"Custom field value {version} already exists and no update is needed.");
        }

        static IEnumerable<JsonObject> getCustomField(JsonDocument doc, string name)
        {
            foreach (var obj in doc.RootElement.EnumerateArray())
            {
                if (obj.TryGetProperty("name", out var nameElement) && nameElement.ValueEquals(name))
                    yield return JsonObject.Create(obj.Clone())!;
            }
        }
    }

    public void Log(IMessage message) => this.log?.Log(message);

    private async Task<YouTrackProjectInfo> GetProjectAsync(string projectName, CancellationToken cancellationToken = default)
    {
        this.LogDebug("Fetching list of projects...");
        await foreach (var project in this.GetProjectsAsync(cancellationToken).ConfigureAwait(false))
        {
            if (project.Name == projectName || project.ShortName == projectName)
            {
                this.LogDebug($"Project {projectName} found: ID={project.Id}, ShortName={project.ShortName}");
                return project;
            }
        }
        throw new YouTrackException($"Project {projectName} not found in YouTrack.");

    }
    private async Task<string> GetFieldIdAsync(YouTrackProjectInfo project, string versionField, CancellationToken cancellationToken = default)
    {
        await foreach (var field in this.EnumerateVersionFieldsAsync(project, cancellationToken).ConfigureAwait(false))
        {
            if (string.Equals(field.Name, versionField, StringComparison.OrdinalIgnoreCase))
            {
                this.LogDebug($"Custom field {versionField} found: ID={field.Id}");
                return field.Id;
            }
        }
        throw new YouTrackException($"YouTrack custom field {versionField} not found in project {project.Name}.");
    }
    private async IAsyncEnumerable<YouTrackField> EnumerateVersionFieldsAsync(YouTrackProjectInfo project, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        this.LogDebug("Fetching list of custom fields...");
        await foreach (var field in this.GetPaginatedListAsync(
                $"admin/projects/{project.Id}/customFields?fields=id,field(name)",
                GetVersionFields,
                cancellationToken
            ).ConfigureAwait(false))
            yield return field;

        IEnumerable<YouTrackField> GetVersionFields(JsonDocument doc)
        {
            foreach (var obj in doc.RootElement.EnumerateArray())
            {
                if (!obj.TryGetProperty("field", out var fieldElement) || fieldElement.ValueKind != JsonValueKind.Object)
                {
                    this.LogDebug($"Unexpected structure of field object: {obj}");
                    continue;
                }

                if (!this.TryGetString(obj, "$type", out var type)
                    ||
                    !this.TryGetString(fieldElement, "name", out var name)
                    ||
                    !this.TryGetString(obj, "id", out var id)) continue;

                if (type != "VersionProjectCustomField")
                {
                    this.LogDebug($"Field {name} is not a VersionProjectCustomField type.");
                    continue;
                }

                yield return new YouTrackField(id, name);
            }
        }
    }

    private async IAsyncEnumerable<T> GetPaginatedListAsync<T>(string relativeUrl, Func<JsonDocument, IEnumerable<T>> getItems, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        const int pageSize = 40;
        var requestUrl = relativeUrl + (relativeUrl.Contains('?') ? "&" : "?");
        int totalCount = 0;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var request = this.CreateRequest(HttpMethod.Get, requestUrl + $"$skip={totalCount}&$top={pageSize}");

            HttpResponseMessage? response = null;
            try
            {
                try
                {
                    response = await GetResponseAsync(request, cancellationToken).ConfigureAwait(false);
                }
                catch (YouTrackException ex) when (ex.ErrorCode == 400 && ex.Error == "invalid_query")
                {
                    // YouTrack returns an invalid query error if you try to query for a Fix version that is not defined in YouTrack.
                    // Returning an empty list in the event of any invalid query error is not optimal, but probably better than spamming the event log with errors.
                    // Ideally, YouTrack would add some way to query that won't raise this as an error, or at least return an easily identifiable error code.
                    yield break;
                }

                using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
                using var doc = await JsonDocument.ParseAsync(responseStream, cancellationToken: cancellationToken).ConfigureAwait(false);

                foreach (var item in getItems(doc))
                    yield return item;

                int count = doc.RootElement.GetArrayLength();
                totalCount += count;

                if (count < pageSize)
                    break;
            }
            finally
            {
                response?.Dispose();
            }
        }
    }
    private static string MakeUrlCanonical(string apiUrl)
    {
        if (string.IsNullOrWhiteSpace(apiUrl))
            throw new ArgumentNullException(nameof(apiUrl));

        if (apiUrl.EndsWith("/api/"))
            return apiUrl;

        if (apiUrl.EndsWith("/api"))
            return apiUrl + "/";

        return apiUrl.TrimEnd('/') + "/api/";
    }
    private HttpRequestMessage CreateRequest(HttpMethod method, string relativeUrl)
    {
        var request = new HttpRequestMessage(method, this.ApiUrl + relativeUrl);
        if (!string.IsNullOrWhiteSpace(this.Token))
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", this.Token);

        request.Headers.Accept.ParseAdd("application/json");
        this.LogDebug($"Making request to {request.RequestUri}...");
        return request;
    }
    private static async Task<HttpResponseMessage> GetResponseAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        HttpResponseMessage? response = null;
        try
        {
            response = await SDK.CreateHttpClient().SendAsync(request, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                try
                {
                    using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
                    if (response.Content.Headers.ContentType?.MediaType == "application/json")
                    {
                        using var doc = await JsonDocument.ParseAsync(responseStream, cancellationToken: cancellationToken).ConfigureAwait(false);
                        var obj = doc.RootElement;
                        var errorMessage = string.Empty;
                        var errorDesc = string.Empty;
                        if (obj.TryGetProperty("error", out var error))
                            errorMessage = error.GetString();
                        if (obj.TryGetProperty("error_description", out var desc))
                            errorDesc = desc.GetString();

                        throw new YouTrackException((int)response.StatusCode, $"{(int)response.StatusCode} - {errorMessage}: {errorDesc}", errorMessage);
                    }
                    else
                    {
                        using var reader = new StreamReader(responseStream, InedoLib.UTF8Encoding);
                        var buffer = new char[8192];
                        int count = await reader.ReadBlockAsync(buffer, cancellationToken).ConfigureAwait(false);
                        if (count > 0)
                            throw new YouTrackException((int)response.StatusCode, new string(buffer, 0, count));
                        else
                            throw new YouTrackException((int)response.StatusCode, response.StatusCode.ToString());
                    }
                }
                finally
                {
                    response.Dispose();
                }
            }

            return response;
        }
        catch
        {
            response?.Dispose();
            throw;
        }
    }

    private bool TryReadCustomFieldValue(JsonObject obj, string customFieldName, [NotNullWhen(true)] out string? value)
    {
        if (obj["customFields"] is not JsonArray fields)
        {
            this.LogDebug($"Property \"customFields\" not found: {obj}");
            value = null;
            return false;
        }

        var match = fields.OfType<JsonObject>().FirstOrDefault(f => (string?)f["name"] == customFieldName);
        if (match == null)
        {
            this.LogDebug($"Property \"{customFieldName}\" not found in customFields: {obj}");
            value = null;
            return false;
        }

        if (match["value"] is not JsonObject valueObj)
        {
            this.LogDebug($"Property \"{customFieldName}\" has no value object: {obj}");
            value = null;
            return false;
        }

        value = (string?)valueObj["name"];
        if (value == null)
            this.LogDebug($"Property \"{customFieldName}\" has no value: {obj}");

        return value != null;
    }
    private bool TryGetString(JsonElement el, string propertyName, [NotNullWhen(true)] out string? value)
    {
        if (!el.TryGetProperty(propertyName, out var property))
        {
            this.LogDebug($"Property \"{propertyName}\" not found.");
            value = null;
            return false;
        }

        value = property.GetString();
        if (string.IsNullOrEmpty(value))
        {
            this.LogDebug($"Property \"{propertyName}\" has no value.");
            return false;
        }

        return true;
    }

    private static async Task<T?> FirstOrDefaultAsync<T>(IAsyncEnumerable<T> values, Predicate<T>? predicate = null)
    {
        await foreach (var v in values.ConfigureAwait(false))
        {
            if (predicate != null && !predicate(v))
                continue;

            return v;
        }

        return default;
    }

    private sealed record YouTrackField(string Id, string Name);
    internal sealed record YouTrackProjectInfo(string Id, string Name, string ShortName);
}
