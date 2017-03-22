using Inedo.BuildMaster.Extensibility.Providers.IssueTracking;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Xml.Linq;

namespace Inedo.BuildMasterExtensions.YouTrack.Legacy
{
    internal sealed class YouTrackSession
    {
        private Uri restUri;
        private string userName;
        private string password;
        private string releaseField;
        private CookieContainer cookies = new CookieContainer();
        private bool connected;

        public YouTrackSession(string baseUrl, string userName, string password, string releaseField)
        {
            if (string.IsNullOrEmpty(baseUrl))
                throw new ArgumentNullException(nameof(baseUrl));

            this.restUri = new Uri(new Uri(baseUrl.EndsWith("/") ? baseUrl : baseUrl + "/"), "rest/");
            this.userName = userName;
            this.password = password;
            this.releaseField = releaseField;
            this.connected = string.IsNullOrEmpty(userName);
        }

        public void Connect()
        {
            if (!this.connected)
            {
                var rubbish = this.DownloadXDocument(
                    WebRequestMethods.Http.Post,
                    "user/login",
                    new KeyValuePair<string, string>("login", this.userName),
                    new KeyValuePair<string, string>("password", this.password)
                );
                this.connected = true;
            }
        }
        public IEnumerable<IssueTrackerCategory> GetProjects()
        {
            this.Connect();

            return this.DownloadXDocument(WebRequestMethods.Http.Get, "project/all")
                .Descendants("project")
                .Select(p => new IssueTrackerCategory((string)p.Attribute("shortName"), (string)p.Attribute("name")));
        }
        public IEnumerable<IssueTrackerIssue> GetIssues(string projectId, string releaseNumber, int maxIssues)
        {
            this.Connect();

            XDocument xdoc;
            if (projectId == YouTrackIssueTrackingProvider.AnyProjectCategory)
            {
                xdoc = this.DownloadXDocument(
                    WebRequestMethods.Http.Get,
                    "issue",
                    new KeyValuePair<string, string>("max", maxIssues.ToString()),
                    new KeyValuePair<string, string>("filter", this.releaseField + ":" + releaseNumber)
                );
            }
            else
            {
                xdoc = this.DownloadXDocument(
                    WebRequestMethods.Http.Get,
                    "issue/byproject/" + Uri.EscapeUriString(projectId),
                    new KeyValuePair<string, string>("max", maxIssues.ToString()),
                    new KeyValuePair<string, string>("filter", this.releaseField + ":" + releaseNumber)
                );
            }

            return xdoc
                    .Descendants("issue")
                    .Select(i => new YouTrackIssue(
                        (string)i.Attribute("id"),
                        (string)i.Descendants("value").FirstOrDefault(v => v.Ancestors("field").Any(f => string.Equals((string)f.Attribute("name"), "State", StringComparison.OrdinalIgnoreCase))),
                        (string)i.Descendants("value").FirstOrDefault(v => v.Ancestors("field").Any(f => string.Equals((string)f.Attribute("name"), "summary", StringComparison.OrdinalIgnoreCase))),
                        (string)i.Descendants("value").FirstOrDefault(v => v.Ancestors("field").Any(f => string.Equals((string)f.Attribute("name"), "description", StringComparison.OrdinalIgnoreCase))),
                        releaseNumber,
                        i.Descendants("value").Any(v => v.Ancestors("field").Any(f => string.Equals((string)f.Attribute("name"), "resolved", StringComparison.OrdinalIgnoreCase)))));
        }
        public void ApplyCommandToIssue(string issueId, string command, string comment = null)
        {
            this.Connect();

            var args = new List<KeyValuePair<string, string>> { new KeyValuePair<string, string>("command", command) };
            if (!string.IsNullOrWhiteSpace(comment))
                args.Add(new KeyValuePair<string, string>("comment", comment));

            this.DownloadXDocument(
                WebRequestMethods.Http.Post,
                "issue/" + Uri.EscapeUriString(issueId) + "/execute",
                args
            );
        }

        private XDocument DownloadXDocument(string method, string address, params KeyValuePair<string, string>[] data)
        {
            return this.DownloadXDocument(method, address, (IEnumerable<KeyValuePair<string, string>>)data);
        }
        private XDocument DownloadXDocument(string method, string address, IEnumerable<KeyValuePair<string, string>> data)
        {
            Uri connectUri;
            if (method == WebRequestMethods.Http.Get && data.Any())
            {
                connectUri = new Uri(
                    this.restUri,
                    address + "?" + string.Join("&", data.Select(a => Uri.EscapeDataString(a.Key) + "=" + Uri.EscapeDataString(a.Value)))
                );
            }
            else
            {
                connectUri = new Uri(this.restUri, address);
            }

            var request = (HttpWebRequest)WebRequest.Create(connectUri);
            request.Method = method;
            request.CookieContainer = this.cookies;
            request.ContentType = "application/x-www-form-urlencoded";

            if (method == WebRequestMethods.Http.Post && data.Any())
            {
                using (var requestStream = request.GetRequestStream())
                using (var writer = new StreamWriter(requestStream, new UTF8Encoding(false)))
                {
                    bool first = true;
                    foreach (var item in data)
                    {
                        if (first)
                            first = false;
                        else
                            writer.Write('&');

                        writer.Write(Uri.EscapeDataString(item.Key));
                        writer.Write('=');
                        writer.Write(Uri.EscapeDataString(item.Value ?? string.Empty));
                    }
                }
            }

            try
            {
                using (var response = (HttpWebResponse)request.GetResponse())
                using (var responseStream = response.GetResponseStream())
                {
                    if (response.ContentLength != 0)
                        return XDocument.Load(responseStream);
                    else
                        return null;
                }
            }
            catch (WebException ex)
            {
                string responseText;

                using (var responseStream = ex.Response.GetResponseStream())
                {
                    responseText = new StreamReader(responseStream).ReadToEnd();
                }

                try
                {
                    var xdoc = XDocument.Parse(responseText);
                    responseText = (string)xdoc.Descendants("error").FirstOrDefault();
                }
                catch
                {
                }

                throw new InvalidOperationException(responseText);
            }
        }
    }
}
