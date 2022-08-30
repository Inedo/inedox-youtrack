using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Inedo.Diagnostics;

namespace Inedo.Extensions.YouTrack
{
    internal sealed class YouTrackClient : ILogSink
    {
        private readonly ILogSink log;

        public YouTrackClient(string token, string apiUrl, ILogSink log = null)
        {
            this.log = log;
            this.Token = token;
            this.ApiUrl = MakeUrlCanonical(apiUrl);
        }

        public string Token { get; }
        public string ApiUrl { get; }

        public IAsyncEnumerable<YouTrackProject> GetProjectsAsync(CancellationToken cancellationToken = default)
        {
            return this.GetPaginatedListAsync("admin/projects?fields=id,name,shortName", getProjects, cancellationToken);

            static IEnumerable<YouTrackProject> getProjects(JsonDocument doc)
            {
                foreach (var obj in doc.RootElement.EnumerateArray())
                {
                    if (obj.TryGetProperty("id", out var idProperty))
                    {
                        var id = idProperty.GetString();
                        string name = null;
                        string shortName = null;

                        if (obj.TryGetProperty("name", out var nameProperty))
                            name = nameProperty.GetString();

                        if (obj.TryGetProperty("shortName", out var shortNameProperty))
                            shortName = shortNameProperty.GetString();

                        yield return new YouTrackProject(id, name, shortName);
                    }
                }
            }
        }
        public async Task<string> CreateIssueAsync(string projectName, string summary, string description, CancellationToken cancellationToken = default)
        {
            YouTrackProject project = null;

            this.LogDebug("Fetching list of projects...");
            await foreach (var p in this.GetProjectsAsync(cancellationToken).ConfigureAwait(false))
            {
                if (p.Name == projectName || p.ShortName == projectName)
                {
                    project = p;
                    break;
                }
            }

            if (project == null)
                throw new YouTrackException($"Project {projectName} not found in YouTrack.");

            this.LogDebug($"Project {projectName} found: ID={project.Id}, ShortName={project.ShortName}");

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

            var id = doc.RootElement.GetProperty("idReadable").GetString();
            this.LogInformation($"Created issue {id}.");
            return id;
        }
        public async IAsyncEnumerable<YouTrackIssue> EnumerateIssuesAsync(string projectName = null, string customQuery = null, string statusField = null, string typeField = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
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
                var id = (string)obj["idReadable"];
                if (!string.IsNullOrWhiteSpace(id))
                {
                    string status = null;
                    if (!string.IsNullOrWhiteSpace(statusField))
                        status = ReadCustomFieldValue(obj, statusField);

                    string type = null;
                    if (!string.IsNullOrWhiteSpace(typeField))
                        type = ReadCustomFieldValue(obj, typeField);

                    yield return new YouTrackIssue(id, obj, baseUrl + id, status, type);
                }
            }

            static IEnumerable<JsonObject> getObjects(JsonDocument doc)
            {
                foreach (var obj in doc.RootElement.EnumerateArray())
                    yield return JsonObject.Create(obj);
            }
        }
        public async Task<IReadOnlyCollection<YouTrackIssue>> GetIssuesAsync(string projectName = null, string customQuery = null, string statusField = null, string typeField = null, CancellationToken cancellationToken = default)
        {
            var issues = new List<YouTrackIssue>();
            await foreach (var issue in EnumerateIssuesAsync(projectName, customQuery, statusField, typeField, cancellationToken).ConfigureAwait(false))
                issues.Add(issue);

            return issues;
        }
        public async Task RunCommandAsync(string command, IEnumerable<string> issueIds, string comment = null, CancellationToken cancellationToken = default)
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
        public async Task EnsureVersionAsync(string versionField, string projectName, string version, bool? released, bool? archived, CancellationToken cancellationToken)
        {
            this.LogDebug("Fetching list of projects...");
            YouTrackProject project = null;

            await foreach (var p in this.GetProjectsAsync(cancellationToken).ConfigureAwait(false))
            {
                if (p.Name == projectName || p.ShortName == projectName)
                {
                    project = p;
                    break;
                }
            }

            if (project == null)
                throw new YouTrackException($"Project {projectName} not found in YouTrack.");

            this.LogDebug($"Project {projectName} found: ID={project.Id}, ShortName={project.ShortName}");

            this.LogDebug("Fetching list of custom fields...");
            var customFieldId = await FirstOrDefaultAsync(
                this.GetPaginatedListAsync(
                    $"admin/projects/{project.Id}/customFields?fields=id,field(name)",
                    d => getFieldId(d, versionField),
                    cancellationToken
                )
            ).ConfigureAwait(false);

            if (string.IsNullOrEmpty(customFieldId))
                throw new YouTrackException($"YouTrack custom field {versionField} not found in project {projectName}.");

            this.LogDebug($"Custom field {versionField} found: ID={customFieldId}");

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

                if ((bool)data["released"] != released || (bool)data["archived"] != archived)
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

                using var request = this.CreateRequest(HttpMethod.Post, $"admin/projects/{project.Id}/customFields/{customFieldId}/bundle/values/{(string)data["id"]}");
                request.Content = new StringContent(data.ToJsonString(), InedoLib.UTF8Encoding, "application/json");

                using var response = await GetResponseAsync(request, cancellationToken).ConfigureAwait(false);
                this.LogInformation($"Version {version} update complete.");
            }
            else
            {
                this.LogInformation($"Custom field value {version} already exists and no update is needed.");
            }
            static IEnumerable<string> getFieldId(JsonDocument doc, string name)
            {
                foreach (var obj in doc.RootElement.EnumerateArray())
                {
                    if (obj.TryGetProperty("field", out var fieldElement) && fieldElement.ValueKind == JsonValueKind.Object)
                    {
                        if (fieldElement.TryGetProperty("name", out var nameElement) && fieldElement.ValueEquals(name))
                        {
                            if (obj.TryGetProperty("id", out var idElement))
                                yield return idElement.GetString();
                        }
                    }
                }
            }

            static IEnumerable<JsonObject> getCustomField(JsonDocument doc, string name)
            {
                foreach (var obj in doc.RootElement.EnumerateArray())
                {
                    if (obj.TryGetProperty("name", out var nameElement) && nameElement.ValueEquals(name))
                        yield return JsonObject.Create(obj);
                }
            }
        }
        public void Log(IMessage message) => this.log?.Log(message);

        private static async Task<T> FirstOrDefaultAsync<T>(IAsyncEnumerable<T> values)
        {
            await foreach (var v in values.ConfigureAwait(false))
                return v;

            return default;
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

                HttpResponseMessage response = null;
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

                    int count = 0;

                    foreach (var item in getItems(doc))
                    {
                        count++;
                        yield return item;
                    }

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
        private HttpRequestMessage CreateRequest(HttpMethod method, string relativeUrl)
        {
            var request = new HttpRequestMessage(method, this.ApiUrl + relativeUrl);
            if (!string.IsNullOrWhiteSpace(this.Token))
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", this.Token);

            request.Headers.Accept.ParseAdd("application/json");
            this.LogDebug($"Making request to {request.RequestUri}...");
            return request;
        }
        private static string ReadCustomFieldValue(JsonObject obj, string customFieldName)
        {
            if (obj["customFields"] is not JsonArray fields)
                return null;

            var match = fields.OfType<JsonObject>().FirstOrDefault(f => (string)f["name"] == customFieldName);
            if (match == null)
                return null;

            return match["value"] is JsonObject valueObj ? (string)valueObj["name"] : null;
        }
        private static async Task<HttpResponseMessage> GetResponseAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            HttpResponseMessage response = null;
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
    }
}
