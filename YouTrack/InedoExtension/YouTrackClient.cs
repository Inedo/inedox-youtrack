using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Inedo.Diagnostics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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

        public async Task<IReadOnlyCollection<YouTrackProject>> GetProjectsAsync(CancellationToken cancellationToken = default)
        {
            var tokens = await this.GetPaginatedListAsync("admin/projects?fields=id,name,shortName", cancellationToken).ConfigureAwait(false);
            var projects = new List<YouTrackProject>(tokens.Count);

            foreach (var obj in tokens.OfType<JObject>())
            {
                var id = (string)obj.Property("id");
                if (!string.IsNullOrWhiteSpace(id))
                {
                    var name = (string)obj.Property("name");
                    var shortName = (string)obj.Property("shortName");
                    projects.Add(new YouTrackProject(id, name, shortName));
                }
            }

            this.LogDebug($"Query returned {projects.Count} projects.");
            return projects;
        }
        public async Task<string> CreateIssueAsync(string projectName, string summary, string description, CancellationToken cancellationToken = default)
        {
            this.LogDebug("Fetching list of projects...");
            var project = (await this.GetProjectsAsync(cancellationToken).ConfigureAwait(false))
                .FirstOrDefault(p => p.Name == projectName || p.ShortName == projectName);

            if (project == null)
                throw new YouTrackException($"Project {projectName} not found in YouTrack.");

            this.LogDebug($"Project {projectName} found: ID={project.Id}, ShortName={project.ShortName}");

            var request = this.CreateRequest("issues?fields=idReadable");
            request.ContentType = "application/json";
            request.Method = "POST";

            using (var writer = new JsonTextWriter(new StreamWriter(await request.GetRequestStreamAsync().ConfigureAwait(false), InedoLib.UTF8Encoding)))
            {
                writer.WriteStartObject();

                writer.WritePropertyName("project");
                writer.WriteStartObject();
                writer.WritePropertyName("id");
                writer.WriteValue(project.Id);
                writer.WriteEndObject();

                writer.WritePropertyName("summary");
                writer.WriteValue(summary);

                writer.WritePropertyName("description");
                writer.WriteValue(description);

                writer.WriteEndObject();
            }

            using var response = await GetResponseAsync(request, cancellationToken).ConfigureAwait(false);
            using var reader = new JsonTextReader(new StreamReader(response.GetResponseStream(), Encoding.UTF8));
            var obj = JObject.Load(reader);

            var id = (string)obj.Property("idReadable");
            this.LogInformation($"Created issue {id}.");
            return id;
        }
        public async Task<IReadOnlyCollection<YouTrackIssue>> GetIssuesAsync(string projectName = null, string customQuery = null, string statusField = null, string typeField = null, CancellationToken cancellationToken = default)
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

            try
            {
                var tokens = await this.GetPaginatedListAsync("issues?fields=id,idReadable,summary,description,reporter(fullName),created,resolved,customFields(name,value(name))&query=" + Uri.EscapeDataString(query.ToString()), cancellationToken).ConfigureAwait(false);
                var issues = new List<YouTrackIssue>(tokens.Count);

                var baseUrl = this.ApiUrl.TrimEnd('/');
                baseUrl = baseUrl.Substring(0, baseUrl.LastIndexOf('/')) + "/issue/";

                foreach (var obj in tokens.OfType<JObject>())
                {
                    var id = (string)obj.Property("idReadable");
                    if (!string.IsNullOrWhiteSpace(id))
                    {
                        string status = null;
                        if (!string.IsNullOrWhiteSpace(statusField))
                            status = ReadCustomFieldValue(obj, statusField);

                        string type = null;
                        if (!string.IsNullOrWhiteSpace(typeField))
                            type = ReadCustomFieldValue(obj, typeField);

                        issues.Add(new YouTrackIssue(id, obj, baseUrl + id, status, type));
                    }
                }

                return issues;
            }
            catch (YouTrackException ex) when (ex.ErrorCode == 400 && ex.Error == "invalid_query")
            {
                // YouTrack returns an invalid query error if you try to query for a Fix version that is not defined in YouTrack.
                // Returning an empty list in the event of any invalid query error is not optimal, but probably better than spamming the event log with errors.
                // Ideally, YouTrack would add some way to query that won't raise this as an error, or at least return an easily identifiable error code.
                return InedoLib.EmptyArray<YouTrackIssue>();
            }
        }
        public async Task RunCommandAsync(string command, IEnumerable<string> issueIds, string comment = null, CancellationToken cancellationToken = default)
        {
            var idList = issueIds as IReadOnlyCollection<string> ?? issueIds.ToList();
            if (idList.Count == 0)
                return;

            var request = this.CreateRequest("commands");
            request.ContentType = "application/json";
            request.Method = "POST";

            using (var writer = new JsonTextWriter(new StreamWriter(await request.GetRequestStreamAsync().ConfigureAwait(false), InedoLib.UTF8Encoding)))
            {
                writer.WriteStartObject();

                writer.WritePropertyName("query");
                writer.WriteValue(command);

                if (!string.IsNullOrWhiteSpace(comment))
                {
                    writer.WritePropertyName("comment");
                    writer.WriteValue(comment);
                }

                writer.WritePropertyName("issues");
                writer.WriteStartArray();
                foreach (var i in idList)
                {
                    writer.WriteStartObject();
                    writer.WritePropertyName("idReadable");
                    writer.WriteValue(i);
                    writer.WriteEndObject();
                }

                writer.WriteEndArray();

                writer.WriteEndObject();
            }

            using var response = await GetResponseAsync(request, cancellationToken).ConfigureAwait(false);

            using var reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
            var rubbish = reader.ReadToEnd();
        }
        public async Task EnsureVersionAsync(string versionField, string projectName, string version, bool? released, bool? archived, CancellationToken cancellationToken)
        {
            this.LogDebug("Fetching list of projects...");
            var project = (await this.GetProjectsAsync(cancellationToken).ConfigureAwait(false))
                .FirstOrDefault(p => p.Name == projectName || p.ShortName == projectName);

            if (project == null)
                throw new YouTrackException($"Project {projectName} not found in YouTrack.");

            this.LogDebug($"Project {projectName} found: ID={project.Id}, ShortName={project.ShortName}");

            this.LogDebug("Fetching list of custom fields...");
            var customField = (await this.GetPaginatedListAsync($"admin/projects/{project.Id}/customFields?fields=id,field(name)", cancellationToken).ConfigureAwait(false))
                .OfType<JObject>()
                .FirstOrDefault(f => (string)(f.Property("field")?.Value as JObject)?.Property("name") == versionField);

            var customFieldId = (string)customField?.Property("id");
            if (string.IsNullOrEmpty(customFieldId))
                throw new YouTrackException($"YouTrack custom field {versionField} not found in project {projectName}.");

            this.LogDebug($"Custom field {versionField} found: ID={customFieldId}");

            this.LogDebug("Fetching custom field values...");
            var customFieldUrl = $"admin/projects/{project.Id}/customFields/{customFieldId}/bundle/values?fields=id,name,released,archived";
            var data = (await this.GetPaginatedListAsync(customFieldUrl, default).ConfigureAwait(false))
                .OfType<JObject>()
                .FirstOrDefault(f => (string)f.Property("name") == version);

            bool update = false;

            if (data == null)
            {
                this.LogDebug($"Custom field value {version} does not already exist.");

                data = new JObject(
                    new JProperty("name", version),
                    new JProperty("released", released ?? false),
                    new JProperty("archived", archived ?? false),
                    new JProperty("type", "$VersionBundleElement")
                );

                update = true;
            }
            else
            {
                this.LogDebug($"Custom field value {version} already exists.");

                if ((bool)data.Property("released") != released || (bool)data.Property("archived") != archived)
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

                var request = this.CreateRequest($"admin/projects/{project.Id}/customFields/{customFieldId}/bundle/values/{(string)data.Property("id")}");
                request.ContentType = "application/json";
                request.Method = "POST";

                using (var writer = new JsonTextWriter(new StreamWriter(await request.GetRequestStreamAsync().ConfigureAwait(false), InedoLib.UTF8Encoding)))
                {
                    data.WriteTo(writer);
                }

                using var response = await GetResponseAsync(request, cancellationToken).ConfigureAwait(false);

                this.LogInformation($"Version {version} update complete.");
            }
            else
            {
                this.LogInformation($"Custom field value {version} already exists and no update is needed.");
            }
        }
        public void Log(IMessage message) => this.log?.Log(message);

        private async Task<IReadOnlyCollection<JToken>> GetPaginatedListAsync(string relativeUrl, CancellationToken cancellationToken)
        {
            const int pageSize = 40;

            var requestUrl = relativeUrl + (relativeUrl.IndexOf('?') >= 0 ? "&" : "?");

            var results = new List<JToken>();

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var request = this.CreateRequest(requestUrl + $"$skip={results.Count}&$top={pageSize}");

                using var response = await GetResponseAsync(request, cancellationToken).ConfigureAwait(false);
                using var reader = new JsonTextReader(new StreamReader(response.GetResponseStream(), Encoding.UTF8));

                var array = JArray.Load(reader);
                if (array.Count > 0)
                    results.AddRange(array);

                if (array.Count < pageSize)
                    return results;
            }
        }
        private HttpWebRequest CreateRequest(string relativeUrl)
        {
            var request = WebRequest.CreateHttp(this.ApiUrl + relativeUrl);
            if (!string.IsNullOrWhiteSpace(this.Token))
                request.Headers.Add("Authorization", "Bearer " + this.Token);

            request.Accept = "application/json";
            request.AutomaticDecompression = DecompressionMethods.GZip;
            request.UserAgent = $"{SDK.ProductName}/{SDK.ProductVersion} (YouTrack/{typeof(YouTrackClient).Assembly.GetName().Version})";
            this.LogDebug($"Making request to {request.RequestUri}...");
            return request;
        }
        private static string ReadCustomFieldValue(JObject obj, string customFieldName)
        {
            if (obj.Property("customFields")?.Value is not JArray fields)
                return null;

            var match = fields.OfType<JObject>().FirstOrDefault(f => (string)f.Property("name") == customFieldName);
            if (match == null)
                return null;

            return match.Property("value")?.Value is JObject valueObj ? (string)valueObj.Property("name") : null;
        }
        private static async Task<WebResponse> GetResponseAsync(HttpWebRequest request, CancellationToken cancellationToken)
        {
            using var reg = cancellationToken.Register(() => request.Abort());
            try
            {
                return await request.GetResponseAsync().ConfigureAwait(false);
            }
            catch (WebException ex) when (ex.Response is HttpWebResponse response)
            {
                if (response.ContentType?.StartsWith("application/json") == true)
                {
                    using var reader = new JsonTextReader(new StreamReader(response.GetResponseStream(), Encoding.UTF8));
                    var obj = JObject.Load(reader);
                    throw new YouTrackException((int)response.StatusCode, obj);
                }
                else
                {
                    using var reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
                    var buffer = new char[8192];
                    int count = reader.ReadBlock(buffer, 0, buffer.Length);
                    if (count > 0)
                        throw new YouTrackException((int)response.StatusCode, new string(buffer, 0, count));
                    else
                        throw new YouTrackException((int)response.StatusCode, response.StatusDescription);
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
    }
}
