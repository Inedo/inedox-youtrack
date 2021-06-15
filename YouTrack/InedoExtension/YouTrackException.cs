using System;
using Newtonsoft.Json.Linq;

namespace Inedo.Extensions.YouTrack
{
    internal sealed class YouTrackException : Exception
    {
        public YouTrackException(string message) : base(message)
        {
        }
        public YouTrackException(int errorCode, string message) : base($"{errorCode} - {message}")
        {
            this.ErrorCode = errorCode;
        }
        public YouTrackException(int errorCode, JObject errorObj) : base(FormatError(errorCode, errorObj))
        {
            this.ErrorCode = errorCode;
            this.Error = (string)errorObj.Property("error");
        }

        public string Error { get; }
        public int? ErrorCode { get; }

        private static string FormatError(int errorCode, JObject errorObj)
        {
            var shortDesc = (string)errorObj.Property("error");
            var longDesc = (string)errorObj.Property("error_description");
            return $"{errorCode} - {shortDesc}: {longDesc}";
        }
    }
}
