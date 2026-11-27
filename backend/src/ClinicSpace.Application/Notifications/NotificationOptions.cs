namespace ClinicSpace.Application.Notifications;

public sealed class NotificationOptions
{
    public const string SectionName = "Notifications";

    /// <summary>Base URL of the web app, used to build links in emails.</summary>
    public string PublicBaseUrl { get; set; } = "http://localhost:8080";

    public string ServiceName { get; set; } = "MPFT Clinic Space";

    /// <summary>Whether to send the requester a confirmation when they submit a request.</summary>
    public bool SendSubmissionConfirmation { get; set; } = true;
}
