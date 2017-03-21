using Inedo.Extensions.YouTrack.Credentials;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace Inedo.Extensions.YouTrack
{
    public sealed class YouTrackClient : IDisposable
    {
#region Internals
        private readonly YouTrackCredentials credentials;
        private readonly HttpClient client;
        private readonly Func<Exception, Task> reauthenticate;

        public YouTrackClient(YouTrackCredentials credentials)
        {
            this.credentials = credentials;
            if (this.credentials.PermanentToken?.Length > 0)
            {
                this.client = new HttpClient();
                this.client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", this.credentials.PermanentToken.ToUnsecureString());
                this.reauthenticate = this.ReauthenticateErrorAsync;
            }
            else if (!string.IsNullOrEmpty(this.credentials.UserName))
            {
                this.client = new HttpClient(new HttpClientHandler() { CookieContainer = new CookieContainer() }, true);
                this.reauthenticate = this.ReauthenticateUserNamePasswordAsync;
            }
            else
            {
                this.client = new HttpClient();
                this.reauthenticate = this.ReauthenticateErrorAsync;
            }
        }

        private async Task<string> RequestUri(string path, HttpContent query = null)
        {
            return this.credentials.ServerUrl.TrimEnd('/') + path + (query != null ? "?" + await query.ReadAsStringAsync().ConfigureAwait(false) : "");
        }

        private async Task ReauthenticateUserNamePasswordAsync(Exception ex)
        {
            using (var response = await this.client.PostAsync(await this.RequestUri("/rest/user/login").ConfigureAwait(false), new FormUrlEncodedContent(new Dictionary<string, string>()
            {
                { "login", this.credentials.UserName },
                { "password", this.credentials.Password.ToUnsecureString() },
            })).ConfigureAwait(false))
            {
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    var error = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    try
                    {
                        var xdoc = XDocument.Parse(error);
                        if (xdoc.Root.Name == "error" && !xdoc.Root.HasElements)
                        {
                            error = xdoc.Root.Value;
                        }
                    }
                    catch
                    {
                        // use the full response as the error message
                    }
                    throw new InvalidOperationException($"Authentication failed for user {this.credentials.UserName} on YouTrack {this.credentials.ServerUrl}: {error}", ex);
                }
            }
        }

        private Task ReauthenticateErrorAsync(Exception ex)
        {
            if (this.credentials.PermanentToken?.Length > 0)
            {
                throw new InvalidOperationException($"Authentication failed for permanent token on YouTrack {this.credentials.ServerUrl}", ex);
            }
            throw new InvalidOperationException($"Authentication failed for anonymous user on YouTrack {this.credentials.ServerUrl}", ex);
        }

        public void Dispose()
        {
            this.client.Dispose();
        }

        private async Task<XDocument> GetAsync(string path, HttpContent query = null)
        {
            return await this.AttemptAsync(path, query, async uri => await this.client.GetAsync(uri).ConfigureAwait(false)).ConfigureAwait(false);
        }

        private async Task<XDocument> DeleteAsync(string path, HttpContent query = null)
        {
            return await this.AttemptAsync(path, query, async uri => await this.client.DeleteAsync(uri).ConfigureAwait(false)).ConfigureAwait(false);
        }

        private async Task<XDocument> PostAsync(string path, HttpContent query = null, HttpContent body = null)
        {
            return await this.AttemptAsync(path, query, async uri => await this.client.PostAsync(uri, body).ConfigureAwait(false)).ConfigureAwait(false);
        }

        private async Task<XDocument> PutAsync(string path, HttpContent query = null, HttpContent body = null)
        {
            return await this.AttemptAsync(path, query, async uri => await this.client.PutAsync(uri, body).ConfigureAwait(false)).ConfigureAwait(false);
        }

        private async Task<XDocument> AttemptAsync(string path, HttpContent query, Func<string, Task<HttpResponseMessage>> request)
        {
            var uri = await this.RequestUri(path, query).ConfigureAwait(false);

            Exception ex = null;

            using (var response = await request(uri).ConfigureAwait(false))
            {
                var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                try
                {
                    var xdoc = XDocument.Parse(content);
                    if (response.StatusCode != HttpStatusCode.Forbidden)
                    {
                        return xdoc;
                    }

                    if (xdoc.Root.Name == "error" && !xdoc.Root.HasElements)
                    {
                        ex = new Exception($"YouTrack request failed: {path} returned {xdoc.Root.Value}");
                    }
                    else
                    {
                        ex = new Exception($"YouTrack request failed: {path} returned {content}");
                    }
                }
                catch (XmlException)
                {
                    throw new InvalidOperationException($"YouTrack request failed: {path} returned {(int)response.StatusCode} {response.ReasonPhrase}: {content}");
                }
            }

            await this.reauthenticate(ex).ConfigureAwait(false);

            using (var response = await request(uri).ConfigureAwait(false))
            {
                var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                try
                {
                    var xdoc = XDocument.Parse(content);
                    if (response.StatusCode != HttpStatusCode.Forbidden)
                    {
                        return xdoc;
                    }

                    if (xdoc.Root.Name == "error" && !xdoc.Root.HasElements)
                    {
                        throw new InvalidOperationException($"YouTrack request failed: {path} returned {xdoc.Root.Value}", ex);
                    }
                    else
                    {
                        throw new InvalidOperationException($"YouTrack request failed: {path} returned {content}", ex);
                    }
                }
                catch (XmlException)
                {
                    throw new InvalidOperationException($"YouTrack request failed: {path} returned {(int)response.StatusCode} {response.ReasonPhrase}: {content}", ex);
                }
            }
        }
#endregion
    }
}
