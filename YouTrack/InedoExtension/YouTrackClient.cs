﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Inedo.Extensibility.Credentials;
using Inedo.Extensibility.IssueSources;
using Inedo.Extensibility.SecureResources;
using Inedo.Extensions.Credentials;
using Inedo.Extensions.YouTrack.Credentials;
using Inedo.Extensions.YouTrack.IssueSources;
using Inedo.Extensions.YouTrack.Operations;
using Inedo.IO;
using UsernamePasswordCredentials = Inedo.Extensions.Credentials.UsernamePasswordCredentials;

namespace Inedo.Extensions.YouTrack
{
    internal sealed class YouTrackClient : IDisposable
    {
#region Internals
        private readonly HttpClient client;
        private readonly Func<Exception, CancellationToken, Task> reauthenticate;
        private bool needFirstAuth = false;
        private readonly SecureString permanentToken;
        private readonly string userName;
        private readonly SecureString password;
        private readonly string serverUrl;

        public YouTrackClient(YouTrackOperationBase operation, ICredentialResolutionContext context)
            : this(operation, null, context) { }
        public YouTrackClient(string resourceName, ICredentialResolutionContext context)
            : this(null, resourceName, context) { }

        private YouTrackClient(YouTrackOperationBase operation, string resourceName, ICredentialResolutionContext context)
        {
            resourceName = resourceName ?? operation?.ResourceName;

            YouTrackSecureResource resource = null;
            SecureCredentials credentials = null;
            if (!string.IsNullOrEmpty(resourceName))
            {
                resource = SecureResource.TryCreate(resourceName, context) as YouTrackSecureResource;
                credentials = resource?.GetCredentials(context);
                if (resource == null)
                {
                    var resCred = ResourceCredentials.TryCreate<LegacyYouTrackResourceCredentials>(resourceName);
                    resource = (YouTrackSecureResource)resCred?.ToSecureResource();
                    credentials = resCred?.ToSecureCredentials();
                }

                if (resource == null)
                    throw new InvalidOperationException($"The resource \"{resourceName}\" was not found.");
            }
            if (!string.IsNullOrEmpty(operation?.CredentialName))
            {
                credentials = SecureCredentials.Create(operation.CredentialName, context);
            }

            this.serverUrl = AH.CoalesceString(operation?.ServerUrl, resource?.ServerUrl);
            if (string.IsNullOrEmpty(this.serverUrl))
                throw new InvalidOperationException("ServerUrl is not set.");

            this.userName = AH.CoalesceString(operation?.UserName, (credentials as UsernamePasswordCredentials)?.UserName);
            this.password = operation?.PermanentToken?.Length > 0 ? operation.PermanentToken : (credentials as UsernamePasswordCredentials)?.Password;
            this.permanentToken = operation?.PermanentToken?.Length > 0 ? operation.PermanentToken : (credentials as YouTrackTokenCredentials)?.PermanentToken;
            

            if (this.permanentToken?.Length > 0)
            {
                this.client = new HttpClient();
                this.client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AH.Unprotect(this.permanentToken));
                this.reauthenticate = this.ReauthenticateErrorAsync;
            }
            else if (!string.IsNullOrEmpty(this.userName))
            {
                this.client = new HttpClient(new HttpClientHandler() { CookieContainer = new CookieContainer() }, true);
                this.reauthenticate = this.ReauthenticateUserNamePasswordAsync;
                this.needFirstAuth = true;
            }
            else
            {
                this.client = new HttpClient();
                this.reauthenticate = this.ReauthenticateErrorAsync;
            }
        }

        private async Task<string> RequestUri(string path, HttpContent query = null)
        {
            return this.serverUrl.TrimEnd('/') + path + (query != null ? "?" + await query.ReadAsStringAsync().ConfigureAwait(false) : "");
        }

        private async Task<Exception> ErrorAsync(string description, HttpResponseMessage response)
        {
            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            try
            {
                var xdoc = XDocument.Parse(content);
                if (xdoc.Root.Name == "error" && !xdoc.Root.HasElements)
                {
                    content = xdoc.Root.Value;
                }
            }
            catch
            {
                // use the full response as the error message
            }
            return new InvalidOperationException($"YouTrack {description} returned {(int)response.StatusCode} {response.ReasonPhrase}: {content}");
        }

        private async Task ReauthenticateUserNamePasswordAsync(Exception ex, CancellationToken cancellationToken)
        {
            var body = new Dictionary<string, string>()
            {
                { "login", this.userName },
                { "password", AH.Unprotect(this.password) },
            };

            using (var response = await this.client.PostAsync(await this.RequestUri("/rest/user/login").ConfigureAwait(false), new FormUrlEncodedContent(body), cancellationToken).ConfigureAwait(false))
            {
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    throw await this.ErrorAsync($"authentication attempt for user {this.userName} on {this.serverUrl}", response).ConfigureAwait(false);
                }
            }
        }

        private Task ReauthenticateErrorAsync(Exception ex, CancellationToken cancellationToken)
        {
            if (this.permanentToken?.Length > 0)
            {
                throw new InvalidOperationException($"Authentication failed for permanent token on YouTrack {this.serverUrl}", ex);
            }
            throw new InvalidOperationException($"Authentication failed for anonymous user on YouTrack {this.serverUrl}", ex);
        }

        public void Dispose()
        {
            this.client.Dispose();
        }

        private async Task<HttpResponseMessage> GetAsync(string path, HttpContent query = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await this.AttemptAsync(path, query, async uri => await this.client.GetAsync(uri, cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
        }

        private async Task<HttpResponseMessage> DeleteAsync(string path, HttpContent query = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await this.AttemptAsync(path, query, async uri => await this.client.DeleteAsync(uri, cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
        }

        private async Task<HttpResponseMessage> PostAsync(string path, HttpContent query = null, HttpContent body = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await this.AttemptAsync(path, query, async uri => await this.client.PostAsync(uri, body, cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
        }

        private async Task<HttpResponseMessage> PutAsync(string path, HttpContent query = null, HttpContent body = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await this.AttemptAsync(path, query, async uri => await this.client.PutAsync(uri, body, cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
        }

        private async Task<HttpResponseMessage> AttemptAsync(string path, HttpContent query, Func<string, Task<HttpResponseMessage>> request, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (this.needFirstAuth)
            {
                await this.reauthenticate(null, cancellationToken).ConfigureAwait(false);
                this.needFirstAuth = false;
            }

            var uri = await this.RequestUri(path, query).ConfigureAwait(false);

            var response = await request(uri).ConfigureAwait(false);
            if (response.StatusCode != HttpStatusCode.Forbidden)
            {
                return response;
            }
            Exception ex;
            using (response)
            {
                var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                try
                {
                    var xdoc = XDocument.Parse(content);

                    if (xdoc.Root.Name == "error" && !xdoc.Root.HasElements)
                    {
                        ex = new InvalidOperationException($"YouTrack request failed: {path} returned {xdoc.Root.Value}");
                    }
                    else
                    {
                        ex = new InvalidOperationException($"YouTrack request failed: {path} returned {content}");
                    }
                }
                catch (XmlException)
                {
                    ex = new InvalidOperationException($"YouTrack request failed: {path} returned {content}");
                }
            }

            await this.reauthenticate(ex, cancellationToken).ConfigureAwait(false);

            return await request(uri).ConfigureAwait(false);
        }
#endregion

        public async Task<string> CreateIssueAsync(string project, string summary, string description, CancellationToken cancellationToken = default(CancellationToken))
        {
            var query = new Dictionary<string, string>()
            {
                { "project", project },
                { "summary", summary },
                { "description", description }
            };

            using (var response = await this.PutAsync("/rest/issue", new FormUrlEncodedContent(query), null, cancellationToken).ConfigureAwait(false))
            {
                if (response.StatusCode == HttpStatusCode.Created)
                {
                    return PathEx.GetFileName(response.Headers.Location.OriginalString);
                }
                throw await this.ErrorAsync("create issue", response).ConfigureAwait(false);
            }
        }

        public async Task RunCommandAsync(string issueId, string command, string comment = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            var body = new Dictionary<string, string>()
            {
                { "command", command }
            };

            if (!string.IsNullOrEmpty(comment))
            {
                body["comment"] = comment;
            }

            using (var response = await this.PostAsync($"/rest/issue/{issueId}/execute", null, new FormUrlEncodedContent(body), cancellationToken).ConfigureAwait(false))
            {
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    return;
                }
                throw await this.ErrorAsync("run command", response).ConfigureAwait(false);
            }
        }

        public async Task<IEnumerable<string>> ListStatesAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            var query = new Dictionary<string, string>()
            {
                { "filter", "State: " }
            };
            using (var response = await this.GetAsync("/rest/issue/intellisense", new FormUrlEncodedContent(query), cancellationToken).ConfigureAwait(false))
            {
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var xdoc = XDocument.Load(await response.Content.ReadAsStreamAsync().ConfigureAwait(false));
                    return xdoc.Root.Element("suggest").Elements("item").Where(e => e.Element("styleClass")?.Value == "field").Select(e => e.Element("option").Value);
                }
                throw await this.ErrorAsync("list states", response).ConfigureAwait(false);
            }
        }

        public async Task<IEnumerable<string>> ListProjectsAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            using (var response = await this.GetAsync("/rest/project/all", null, cancellationToken).ConfigureAwait(false))
            {
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var xdoc = XDocument.Load(await response.Content.ReadAsStreamAsync().ConfigureAwait(false));
                    return xdoc.Root.Elements("project").Select(e => e.Attribute("shortName").Value);
                }
                throw await this.ErrorAsync("list projects", response).ConfigureAwait(false);
            }
        }

        public async Task<IEnumerable<IIssueTrackerIssue>> IssuesByProjectAsync(string projectName, string filter = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            var query = new Dictionary<string, string>()
            {
                { "max", "1000000" },
                { "wikifyDescription", "true" }
            };
            if (!string.IsNullOrEmpty(filter))
            {
                query["filter"] = filter;
            }

            using (var response = await this.GetAsync($"/rest/issue/byproject/{projectName}", new FormUrlEncodedContent(query), cancellationToken).ConfigureAwait(false))
            {
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var xdoc = XDocument.Load(await response.Content.ReadAsStreamAsync().ConfigureAwait(false));
                    return xdoc.Root.Elements("issue").Select(node => new YouTrackIssue(this.serverUrl, node));
                }
                throw await this.ErrorAsync("get issues by project", response).ConfigureAwait(false);
            }
        }
    }
}
