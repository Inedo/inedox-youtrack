#nullable enable

using System;

namespace Inedo.Extensions.YouTrack;

internal sealed class YouTrackException : Exception
{
    public YouTrackException(string message) : base(message)
    {
    }
    public YouTrackException(int errorCode, string message, string? error = null) : base($"{errorCode} - {message}")
    {
        this.Error = error;
        this.ErrorCode = errorCode;
    }

    public string? Error { get; }
    public int? ErrorCode { get; }
}
