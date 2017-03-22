using System;

namespace Inedo.OtterExtensions.YouTrack
{
    /// <summary>
    /// Describes an issue in an issue tracker.
    /// </summary>
    public interface IIssueTrackerIssue
    {
        /// <summary>
        /// Gets the unique ID of the issue.
        /// </summary>
        string Id { get; }
        /// <summary>
        /// Gets the current status of the issue.
        /// </summary>
        string Status { get; }
        /// <summary>
        /// Gets the type of the issue (e.g. bug, feature, task).
        /// </summary>
        string Type { get; }
        /// <summary>
        /// Gets the HTML title of the issue.
        /// </summary>
        string Title { get; }
        /// <summary>
        /// Gets the HTML description of the issue.
        /// </summary>
        string Description { get; }
        /// <summary>
        /// Gets the name of the user that submitted the issue.
        /// </summary>
        string Submitter { get; }
        /// <summary>
        /// Gets the UTC date when the issue was submitted.
        /// </summary>
        DateTime SubmittedDate { get; }
        /// <summary>
        /// Gets a value indicating whether the issue is considered closed.
        /// </summary>
        bool IsClosed { get; }
        /// <summary>
        /// Gets the URL of the issue in the original issue tracker if applicable.
        /// </summary>
        string Url { get; }
    }
}
